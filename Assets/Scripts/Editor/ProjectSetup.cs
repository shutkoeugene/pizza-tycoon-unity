#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PizzaTycoon.Editor
{
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [InitializeOnLoadMethod]
        static void AutoSetup()
        {
            EditorApplication.delayCall += () => { if (!File.Exists(ScenePath)) CreateScene(); };
        }

        [UnityEditor.MenuItem("Pizza/Create Scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cgo = new GameObject("Main Camera");
            cgo.tag = "MainCamera";
            cgo.AddComponent<Camera>();
            cgo.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Personal";
            PlayerSettings.productName = "Pizza Tycoon";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();
        }

        [UnityEditor.MenuItem("Pizza/Build Windows")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            UrpSetup.Apply();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/PizzaTycoon.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            if (Application.isBatchMode)
                EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
#endif
