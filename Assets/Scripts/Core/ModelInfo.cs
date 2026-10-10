using System.Collections;
using UnityEngine;

namespace PizzaTycoon
{
    // "-modelinfo": logs bounds of imported third-party models so they can be scaled correctly.
    public class ModelInfo : MonoBehaviour
    {
        public IEnumerator Run()
        {
            yield return null;
            string[] names = {
                "commercial/building-a", "commercial/building-skyscraper-a", "commercial/low-detail-building-a", "commercial/detail-awning", "commercial/detail-parasol-a",
                "suburban/building-type-a", "suburban/tree-large", "suburban/fence", "roads/road-straight", "roads/road-crossroad", "roads/road-bend", "roads/light-square",
                "food/pizza", "furniture/table", "furniture/chair", "furniture/kitchenStove" };
            foreach (var n in names)
            {
                var prefab = Resources.Load<GameObject>("Kenney/" + n);
                if (prefab == null) { Debug.Log("[modelinfo] MISSING " + n); continue; }
                var go = Instantiate(prefab);
                var b = new Bounds(go.transform.position, Vector3.zero); bool first = true;
                foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                var mat = go.GetComponentInChildren<Renderer>().sharedMaterial;
                Debug.Log("[modelinfo] " + n + " size=" + b.size.ToString("0.00") + " center=" + b.center.ToString("0.00") + " shader=" + (mat != null ? mat.shader.name : "null") + " tex=" + (mat != null && mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null));
                Destroy(go);
            }
            Application.Quit();
        }
    }
}
