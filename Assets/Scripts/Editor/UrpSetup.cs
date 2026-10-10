#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PizzaTycoon.Editor
{
    public static class UrpSetup
    {
        const string Dir = "Assets/Settings";

        public static void Apply()
        {
            Directory.CreateDirectory(Dir);
            var assetPath = Dir + "/PizzaURP.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rd, Dir + "/PizzaRenderer.asset");
                asset = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
            }
            asset.shadowDistance = 150f;
            asset.msaaSampleCount = 4;
            asset.supportsHDR = true;
            var so = new SerializedObject(asset);
            Set(so, "m_SoftShadowsSupported", true);
            Set(so, "m_MainLightShadowmapResolution", 4096);
            Set(so, "m_ShadowCascadeCount", 3);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);

            GraphicsSettings.defaultRenderPipeline = asset;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            // make sure the URP shader is part of the player
            var gs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var arr = gs.FindProperty("m_AlwaysIncludedShaders");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (arr != null && lit != null)
            {
                bool has = false;
                for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == lit) has = true;
                if (!has) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = lit; gs.ApplyModifiedProperties(); }
            }
            AssetDatabase.SaveAssets();
        }

        static void Set(SerializedObject so, string name, object v)
        {
            var p = so.FindProperty(name);
            if (p == null) return;
            if (v is bool b) p.boolValue = b; else if (v is int n) p.intValue = n;
        }
    }
}
#endif
