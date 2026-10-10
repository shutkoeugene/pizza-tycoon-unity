using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon
{
    // Loads the free CC0 Kenney model packs from Assets/Resources/Kenney and places them with a given size.
    public static class Kenney
    {
        static int available = -1;
        public static bool Available
        {
            get
            {
                if (available < 0) available = Resources.Load<GameObject>("Kenney/commercial/building-a") != null ? 1 : 0;
                return available == 1;
            }
        }

        static readonly Dictionary<string, Bounds> boundsCache = new Dictionary<string, Bounds>();

        // Places the model so its base centre is at localPos. Scales so the larger horizontal side equals `footprint`
        // (or, when byHeight is true, so the height equals `size`). Returns the holder (null when the model is missing).
        public static Transform Spawn(string path, Transform parent, Vector3 localPos, float yaw, float size, bool byHeight = false, string name = null)
        {
            var prefab = Resources.Load<GameObject>("Kenney/" + path);
            if (prefab == null) return null;
            var holder = new GameObject(name ?? path.Substring(path.LastIndexOf('/') + 1)).transform;
            var inst = Object.Instantiate(prefab, holder, false);
            Bounds b;
            if (!boundsCache.TryGetValue(path, out b))
            {
                bool first = true; b = new Bounds();
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                }
                boundsCache[path] = b;
            }
            float basis = byHeight ? Mathf.Max(0.001f, b.size.y) : Mathf.Max(0.001f, Mathf.Max(b.size.x, b.size.z));
            float s = size / basis;
            inst.transform.localScale = Vector3.one * s;
            inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z) * s;
            holder.SetParent(parent, false);
            holder.localPosition = localPos;
            holder.localRotation = Quaternion.Euler(0, yaw, 0);
            LastSize = b.size * s;
            return holder;
        }

        public static Vector3 LastSize;
    }
}
