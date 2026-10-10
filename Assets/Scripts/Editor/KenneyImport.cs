#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PizzaTycoon.Editor
{
    // Kenney's FBX files import with the Standard shader; switch their materials to URP/Lit
    // and attach the pack's shared palette texture (the kits use one atlas for every model).
    public class KenneyImport : AssetPostprocessor
    {
        public override uint GetVersion() { return 4; }

        void OnPostprocessMaterial(Material m)
        {
            if (!assetPath.Contains("Resources/Kenney")) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return;
            Texture tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Color col = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            if (tex == null)
            {
                string dir = assetPath.Substring(0, assetPath.LastIndexOf('/'));
                string[] candidates = { dir + "/Textures/colormap.png", dir + "/Textures/variation-a.png" };
                foreach (var c in candidates)
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture>(c);
                    if (tex != null) break;
                }
            }
            m.shader = lit;
            if (tex != null) { m.SetTexture("_BaseMap", tex); col = Color.white; }
            m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.2f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        }
    }
}
#endif
