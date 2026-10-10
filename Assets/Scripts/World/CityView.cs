using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon
{
    public class DistrictTag : MonoBehaviour { public int index; }
    public class RestaurantTag : MonoBehaviour { public int restaurantId; }

    public class Spinner : MonoBehaviour
    {
        public float speed = 40f;
        void Update() { transform.Rotate(0, speed * Time.deltaTime, 0, Space.World); }
    }

    public class Bobber : MonoBehaviour
    {
        float baseY, ph;
        void Start() { baseY = transform.localPosition.y; ph = Random.value * 6f; }
        void Update() { var p = transform.localPosition; p.y = baseY + Mathf.Sin(Time.time * 2f + ph) * 0.15f; transform.localPosition = p; }
    }

    public class Car : MonoBehaviour
    {
        public Vector3 dir; public float speed; public float min, max; public bool alongX;
        void Update()
        {
            transform.position += dir * speed * Time.deltaTime;
            float v = alongX ? transform.position.x : transform.position.z;
            if (v > max) transform.position -= dir * (max - min);
            if (v < min) transform.position += dir * (max - min);
        }
    }

    public static class Mats
    {
        static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();
        public static Material Get(Color c, float smooth = 0.25f, float metal = 0f, Color? emission = null)
        {
            int key = c.GetHashCode() ^ (smooth.GetHashCode() * 31) ^ (metal.GetHashCode() * 17) ^ (emission.HasValue ? emission.Value.GetHashCode() * 13 : 0);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            bool urp = sh != null;
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Diffuse");
            m = new Material(sh) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (emission.HasValue && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            cache[key] = m;
            return m;
        }
    }

    public class CityView : MonoBehaviour
    {
        public const float Cell = 26f;
        public const float Plot = 20f;

        Transform root;
        Light sun;
        readonly List<Transform> districtTiles = new List<Transform>();
        readonly List<Renderer> tileRenderers = new List<Renderer>();
        readonly List<Light> nightLights = new List<Light>();
        GameState state;
        int selected = -1;

        public Vector3 DistrictCenter(int i)
        {
            var d = state.City.districts[i];
            return new Vector3(d.cell.x * Cell, 0, d.cell.y * Cell);
        }

        public Vector3 CityCenter()
        {
            var c = state.City;
            return new Vector3((c.districtsX - 1) * Cell * 0.5f, 0, (c.districtsY - 1) * Cell * 0.5f);
        }

        public void SetupEnvironment()
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(transform);
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.intensity = 1.1f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 380f;
            QualitySettings.shadowDistance = 160f;
            
        }

        // dayFraction 0..1 : 0=dawn
        public void SetTimeOfDay(float f)
        {
            if (sun == null) return;
            float ang = Mathf.Lerp(-10f, 190f, f);
            sun.transform.rotation = Quaternion.Euler(Mathf.Clamp(ang, 5f, 175f) > 90 ? 180 - Mathf.Clamp(ang, 5f, 175f) : Mathf.Clamp(ang, 5f, 175f), -35f, 0);
            float h = Mathf.Sin(Mathf.Clamp01(f) * Mathf.PI);
            sun.intensity = Mathf.Lerp(0.15f, 1.15f, Mathf.Pow(h, 0.6f));
            sun.color = Color.Lerp(new Color(1f, 0.55f, 0.3f), new Color(1f, 0.96f, 0.88f), Mathf.Clamp01(h * 1.6f));
            var sky = Color.Lerp(new Color(0.08f, 0.1f, 0.2f), new Color(0.55f, 0.72f, 0.95f), Mathf.Clamp01(h * 1.4f));
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = sky * 0.7f;
            RenderSettings.fogColor = sky;
            if (Camera.main != null) { Camera.main.backgroundColor = sky; Camera.main.clearFlags = CameraClearFlags.SolidColor; }
            bool night = h < 0.28f;
            foreach (var l in nightLights) if (l != null) l.enabled = night;
        }

        GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(t);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Destroy(go.GetComponent<Collider>());
            return go;
        }

        public void Rebuild(GameState s)
        {
            state = s;
            if (root != null) Destroy(root.gameObject);
            districtTiles.Clear(); tileRenderers.Clear(); nightLights.Clear();
            root = new GameObject("City").transform;
            root.SetParent(transform, false);
            var city = s.City;
            var rng = new System.Random(s.cityIndex * 1000 + 17);

            // ground
            float w = city.districtsX * Cell + 120, h = city.districtsY * Cell + 120;
            var ground = Prim(PrimitiveType.Cube, root, CityCenter() + new Vector3(0, -0.6f, 0), new Vector3(w * 2, 1f, h * 2), Mats.Get(Color.Lerp(city.accent, new Color(0.25f, 0.33f, 0.2f), 0.75f), 0.05f));
            ground.name = "Ground";
            // water ring decoration for harbour-like feel
            Prim(PrimitiveType.Cube, root, CityCenter() + new Vector3(0, -0.5f, -(city.districtsY * Cell * 0.5f + 55)), new Vector3(w * 2, 0.8f, 60f), Mats.Get(new Color(0.15f, 0.35f, 0.6f), 0.9f, 0.1f));

            // roads: asphalt slab per cell grid
            var asphalt = Mats.Get(new Color(0.16f, 0.16f, 0.18f), 0.1f);
            var line = Mats.Get(new Color(0.9f, 0.85f, 0.4f), 0.1f);
            float x0 = -Cell * 0.5f, x1 = (city.districtsX - 0.5f) * Cell;
            float z0 = -Cell * 0.5f, z1 = (city.districtsY - 0.5f) * Cell;
            for (int i = 0; i <= city.districtsX; i++)
            {
                float x = x0 + i * Cell;
                Prim(PrimitiveType.Cube, root, new Vector3(x, 0.02f, (z0 + z1) * 0.5f), new Vector3(Cell - Plot, 0.05f, z1 - z0 + (Cell - Plot)), asphalt);
                Prim(PrimitiveType.Cube, root, new Vector3(x, 0.06f, (z0 + z1) * 0.5f), new Vector3(0.2f, 0.02f, z1 - z0), line);
            }
            for (int j = 0; j <= city.districtsY; j++)
            {
                float z = z0 + j * Cell;
                Prim(PrimitiveType.Cube, root, new Vector3((x0 + x1) * 0.5f, 0.02f, z), new Vector3(x1 - x0 + (Cell - Plot), 0.05f, Cell - Plot), asphalt);
                Prim(PrimitiveType.Cube, root, new Vector3((x0 + x1) * 0.5f, 0.06f, z), new Vector3(x1 - x0, 0.02f, 0.2f), line);
            }

            // traffic
            for (int i = 0; i <= city.districtsX; i++)
                for (int k = 0; k < 2; k++) SpawnCar(x0 + i * Cell + (k == 0 ? -1.2f : 1.2f), z0, z1, false, k == 0 ? 1 : -1, rng);
            for (int j = 0; j <= city.districtsY; j++)
                for (int k = 0; k < 2; k++) SpawnCar(z0 + j * Cell + (k == 0 ? -1.2f : 1.2f), x0, x1, true, k == 0 ? 1 : -1, rng);

            // districts
            for (int i = 0; i < city.districts.Count; i++) BuildDistrict(i, rng);

            Highlight(selected);
        }

        void SpawnCar(float fixedCoord, float min, float max, bool alongX, int dirSign, System.Random rng)
        {
            int n = 3;
            for (int i = 0; i < n; i++)
            {
                var col = Color.HSVToRGB((float)rng.NextDouble(), 0.6f, 0.9f);
                var go = new GameObject("Car");
                go.transform.SetParent(root, false);
                float t = Mathf.Lerp(min, max, (float)rng.NextDouble());
                go.transform.position = alongX ? new Vector3(t, 0.5f, fixedCoord) : new Vector3(fixedCoord, 0.5f, t);
                go.transform.rotation = alongX ? Quaternion.Euler(0, dirSign > 0 ? 90 : -90, 0) : Quaternion.Euler(0, dirSign > 0 ? 0 : 180, 0);
                Prim(PrimitiveType.Cube, go.transform, new Vector3(0, 0.1f, 0), new Vector3(1.0f, 0.5f, 2.0f), Mats.Get(col, 0.7f, 0.3f));
                Prim(PrimitiveType.Cube, go.transform, new Vector3(0, 0.5f, -0.1f), new Vector3(0.85f, 0.4f, 1.0f), Mats.Get(new Color(0.15f, 0.2f, 0.28f), 0.9f));
                var c = go.AddComponent<Car>();
                c.alongX = alongX; c.min = min; c.max = max; c.speed = 4f + (float)rng.NextDouble() * 4f;
                c.dir = alongX ? new Vector3(dirSign, 0, 0) : new Vector3(0, 0, dirSign);
            }
        }

        static readonly Vector2[] slots = { new Vector2(-6.5f, -6.5f), new Vector2(6.5f, -6.5f), new Vector2(-6.5f, 6.5f), new Vector2(6.5f, 6.5f) };

        void BuildDistrict(int idx, System.Random rng)
        {
            var d = state.City.districts[idx];
            var dr = new GameObject("District_" + d.name).transform;
            dr.SetParent(root, false);
            dr.localPosition = DistrictCenter(idx);

            var pavement = Mats.Get(Color.Lerp(new Color(0.55f, 0.55f, 0.52f), new Color(0.7f, 0.68f, 0.62f), d.wealth), 0.1f);
            var tile = Prim(PrimitiveType.Cube, dr, new Vector3(0, 0.08f, 0), new Vector3(Plot, 0.2f, Plot), pavement, true);
            tile.AddComponent<DistrictTag>().index = idx;
            districtTiles.Add(tile.transform);
            tileRenderers.Add(tile.GetComponent<Renderer>());

            var restaurants = state.restaurants.FindAll(r => r.district == idx);
            var used = new bool[4];
            // free slots for restaurants, rest buildings
            for (int s = 0; s < 4; s++)
            {
                Vector3 p = new Vector3(slots[s].x, 0.18f, slots[s].y);
                if (s < restaurants.Count)
                {
                    BuildRestaurant(dr, p, restaurants[s], s);
                    used[s] = true;
                }
                else if (rng.NextDouble() < 0.12) { /* empty plot with park */ BuildPark(dr, p, rng); }
                else BuildBlock(dr, p, d, rng);
            }
            // central plaza / trees
            Prim(PrimitiveType.Cylinder, dr, new Vector3(0, 0.3f, 0), new Vector3(3.4f, 0.1f, 3.4f), Mats.Get(new Color(0.7f, 0.7f, 0.68f), 0.2f));
            Prim(PrimitiveType.Cylinder, dr, new Vector3(0, 0.7f, 0), new Vector3(0.9f, 0.4f, 0.9f), Mats.Get(new Color(0.3f, 0.55f, 0.75f), 0.9f));
            for (int i = 0; i < 6; i++)
            {
                float ang = i / 6f * Mathf.PI * 2f + 0.3f;
                Tree(dr, new Vector3(Mathf.Cos(ang) * 3.3f, 0.18f, Mathf.Sin(ang) * 3.3f), rng);
            }
        }

        void Tree(Transform parent, Vector3 pos, System.Random rng)
        {
            float s = 0.8f + (float)rng.NextDouble() * 0.6f;
            Prim(PrimitiveType.Cylinder, parent, pos + new Vector3(0, 0.6f * s, 0), new Vector3(0.25f, 0.6f * s, 0.25f), Mats.Get(new Color(0.35f, 0.22f, 0.12f), 0.05f));
            var g = Color.Lerp(new Color(0.15f, 0.5f, 0.2f), new Color(0.3f, 0.65f, 0.25f), (float)rng.NextDouble());
            Prim(PrimitiveType.Sphere, parent, pos + new Vector3(0, 1.6f * s, 0), new Vector3(1.4f, 1.5f, 1.4f) * s, Mats.Get(g, 0.1f));
        }

        void BuildPark(Transform parent, Vector3 p, System.Random rng)
        {
            Prim(PrimitiveType.Cube, parent, p + new Vector3(0, 0.05f, 0), new Vector3(8f, 0.12f, 8f), Mats.Get(new Color(0.3f, 0.6f, 0.25f), 0.05f));
            for (int i = 0; i < 4; i++)
                Tree(parent, p + new Vector3((float)(rng.NextDouble() - 0.5) * 6f, 0.1f, (float)(rng.NextDouble() - 0.5) * 6f), rng);
        }

        void BuildBlock(Transform parent, Vector3 p, DistrictDef d, System.Random rng)
        {
            // cluster of 2x2 buildings
            for (int ix = 0; ix < 2; ix++)
                for (int iz = 0; iz < 2; iz++)
                {
                    float bw = 2.8f + (float)rng.NextDouble() * 0.8f;
                    float bd = 2.8f + (float)rng.NextDouble() * 0.8f;
                    float hmax = 3f + d.population * 6f + d.wealth * 9f;
                    float bh = Mathf.Lerp(2.5f, hmax, (float)rng.NextDouble());
                    var wall = Color.HSVToRGB((float)rng.NextDouble() * 0.12f + 0.05f, Mathf.Lerp(0.15f, 0.05f, d.wealth), Mathf.Lerp(0.7f, 0.9f, (float)rng.NextDouble()));
                    if (d.wealth > 0.85f && rng.NextDouble() < 0.6) wall = Color.HSVToRGB(0.58f, 0.15f, 0.8f);
                    var pos = p + new Vector3((ix - 0.5f) * 3.8f, 0.1f, (iz - 0.5f) * 3.8f);
                    var b = Prim(PrimitiveType.Cube, parent, pos + new Vector3(0, bh * 0.5f, 0), new Vector3(bw, bh, bd), Mats.Get(wall, 0.3f));
                    // roof
                    Prim(PrimitiveType.Cube, parent, pos + new Vector3(0, bh + 0.1f, 0), new Vector3(bw + 0.2f, 0.2f, bd + 0.2f), Mats.Get(new Color(0.35f, 0.3f, 0.3f), 0.1f));
                    // windows (emissive at night through Mat with emission)
                    int floors = Mathf.Max(1, Mathf.FloorToInt(bh / 1.4f));
                    var win = Mats.Get(new Color(0.2f, 0.3f, 0.4f), 0.95f, 0.2f, new Color(0.9f, 0.75f, 0.35f) * 0.7f);
                    for (int f = 0; f < floors; f++)
                    {
                        float wy = 1.0f + f * 1.4f;
                        if (wy > bh - 0.4f) break;
                        Prim(PrimitiveType.Cube, parent, pos + new Vector3(0, wy, -bd * 0.5f - 0.02f), new Vector3(bw * 0.7f, 0.5f, 0.05f), win);
                        Prim(PrimitiveType.Cube, parent, pos + new Vector3(bw * 0.5f + 0.02f, wy, 0), new Vector3(0.05f, 0.5f, bd * 0.7f), win);
                    }
                }
        }

        static readonly Color[] styleWalls =
        {
            new Color(0.45f, 0.45f, 0.5f),   // roh / loft
            new Color(0.92f, 0.85f, 0.7f),   // einf / simple
            new Color(0.72f, 0.45f, 0.3f),   // rust
            new Color(0.85f, 0.9f, 0.95f),   // modern
            new Color(0.95f, 0.9f, 0.75f),   // elite
        };

        void BuildRestaurant(Transform parent, Vector3 p, Restaurant r, int slot)
        {
            float sc = 1f + r.size * 0.25f;
            float bw = 5.2f * (0.85f + r.size * 0.1f), bd = 5.2f * (0.85f + r.size * 0.1f);
            float bh = 3.2f + r.size * 0.9f + (r.style >= 3 ? 0.8f : 0f);
            var holder = new GameObject("Restaurant_" + r.id).transform;
            holder.SetParent(parent, false);
            holder.localPosition = p;
            var tag = holder.gameObject.AddComponent<RestaurantTag>(); tag.restaurantId = r.id;

            var ownerColor = state.companies[r.owner].color;
            var wall = styleWalls[r.style];
            var body = Prim(PrimitiveType.Cube, holder, new Vector3(0, bh * 0.5f, 0), new Vector3(bw, bh, bd), Mats.Get(wall, r.style >= 3 ? 0.7f : 0.2f, r.style == 4 ? 0.2f : 0f), true);
            body.AddComponent<RestaurantTag>().restaurantId = r.id;
            // roof by style
            var roofMat = Mats.Get(r.style == 2 ? new Color(0.6f, 0.25f, 0.18f) : new Color(0.25f, 0.25f, 0.3f), 0.2f);
            if (r.style == 2 || r.style == 1)
            {
                var roof = Prim(PrimitiveType.Cube, holder, new Vector3(0, bh + 0.5f, 0), new Vector3(bw * 0.9f, 1f, bd * 0.9f), roofMat);
                roof.transform.localRotation = Quaternion.Euler(0, 45, 0);
            }
            else Prim(PrimitiveType.Cube, holder, new Vector3(0, bh + 0.15f, 0), new Vector3(bw + 0.3f, 0.3f, bd + 0.3f), roofMat);

            // glass front
            Prim(PrimitiveType.Cube, holder, new Vector3(0, 1.3f, -bd * 0.5f - 0.03f), new Vector3(bw * 0.8f, 1.8f, 0.06f), Mats.Get(new Color(0.55f, 0.8f, 0.9f), 0.98f, 0.3f, new Color(1f, 0.8f, 0.4f) * 0.35f));
            // awning in owner colour
            var aw = Prim(PrimitiveType.Cube, holder, new Vector3(0, 2.5f, -bd * 0.5f - 0.7f), new Vector3(bw * 0.95f, 0.12f, 1.6f), Mats.Get(ownerColor, 0.3f));
            aw.transform.localRotation = Quaternion.Euler(18, 0, 0);
            // outdoor tables
            int tables = 1 + r.size;
            for (int t = 0; t < tables; t++)
            {
                float tx = (t - (tables - 1) * 0.5f) * 1.8f;
                Prim(PrimitiveType.Cylinder, holder, new Vector3(tx, 0.5f, -bd * 0.5f - 1.9f), new Vector3(0.9f, 0.05f, 0.9f), Mats.Get(Color.white, 0.4f));
                Prim(PrimitiveType.Cylinder, holder, new Vector3(tx, 0.25f, -bd * 0.5f - 1.9f), new Vector3(0.1f, 0.25f, 0.1f), Mats.Get(new Color(0.2f, 0.2f, 0.2f)));
            }
            // pizza sign (rotating)
            var sign = new GameObject("Sign").transform;
            sign.SetParent(holder, false);
            sign.localPosition = new Vector3(0, bh + 2.2f + (r.style >= 2 ? 0.8f : 0), 0);
            sign.gameObject.AddComponent<Spinner>();
            sign.gameObject.AddComponent<Bobber>();
            Prim(PrimitiveType.Cylinder, sign, Vector3.zero, new Vector3(2.2f, 0.12f, 2.2f), Mats.Get(new Color(0.95f, 0.75f, 0.35f), 0.3f));
            Prim(PrimitiveType.Cylinder, sign, new Vector3(0, 0.08f, 0), new Vector3(1.8f, 0.05f, 1.8f), Mats.Get(new Color(0.85f, 0.2f, 0.12f), 0.4f));
            var topCol = Mats.Get(new Color(1f, 0.9f, 0.4f), 0.4f);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Prim(PrimitiveType.Sphere, sign, new Vector3(Mathf.Cos(a) * 0.55f, 0.15f, Mathf.Sin(a) * 0.55f), Vector3.one * 0.3f, i % 2 == 0 ? Mats.Get(new Color(0.65f, 0.12f, 0.12f)) : topCol);
            }
            // owner flag
            Prim(PrimitiveType.Cylinder, holder, new Vector3(bw * 0.4f, bh + 1.2f, bd * 0.4f), new Vector3(0.06f, 1.2f, 0.06f), Mats.Get(Color.gray));
            Prim(PrimitiveType.Cube, holder, new Vector3(bw * 0.4f + 0.5f, bh + 2.0f, bd * 0.4f), new Vector3(1f, 0.6f, 0.05f), Mats.Get(ownerColor, 0.2f));

            // night light
            var lgo = new GameObject("Light");
            lgo.transform.SetParent(holder, false);
            lgo.transform.localPosition = new Vector3(0, 3f, -bd * 0.5f - 2f);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point; l.range = 12f; l.intensity = 1.4f; l.color = new Color(1f, 0.82f, 0.5f);
            l.enabled = false;
            nightLights.Add(l);
        }

        public void Highlight(int idx)
        {
            selected = idx;
            for (int i = 0; i < tileRenderers.Count; i++)
            {
                var m = tileRenderers[i].sharedMaterial;
                tileRenderers[i].material = new Material(m);
                if (i == idx)
                {
                    tileRenderers[i].material.color = new Color(1f, 0.85f, 0.35f);
                    if (tileRenderers[i].material.HasProperty("_BaseColor")) tileRenderers[i].material.SetColor("_BaseColor", new Color(1f, 0.85f, 0.35f));
                    tileRenderers[i].material.EnableKeyword("_EMISSION");
                    tileRenderers[i].material.SetColor("_EmissionColor", new Color(0.35f, 0.25f, 0.05f));
                }
            }
        }
    }
}
