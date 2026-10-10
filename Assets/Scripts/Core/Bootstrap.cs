using UnityEngine;

namespace PizzaTycoon
{
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (Object.FindObjectOfType<GameController>() != null) return;
            Application.targetFrameRate = 60;
            var cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
                cgo.AddComponent<AudioListener>();
            }
            cam.fieldOfView = 40f; cam.nearClipPlane = 0.5f; cam.farClipPlane = 600f;
            cam.allowHDR = true;
            PostFx.Setup(cam);
            var rig = cam.gameObject.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();

            var root = new GameObject("Game");
            var gc = root.AddComponent<GameController>();
            var city = root.AddComponent<CityView>();
            city.SetupEnvironment();
            var ui = root.AddComponent<GameUI>();
            gc.City = city; gc.Rig = rig; gc.UI = ui;
            GameController.I = gc;
            rig.Init(Vector3.zero, new Vector3(-20, 0, -20), new Vector3(120, 0, 120));
            ui.Init();
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a.StartsWith("-uidump=")) { var ud = root.AddComponent<UiDump>(); ud.path = a.Substring(8); }
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-playtest") { var pt = root.AddComponent<PlayTest>(); pt.StartCoroutine(pt.Run()); }
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-uitest") { var ut = root.AddComponent<UiTest>(); ut.StartCoroutine(ut.Run()); }
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-autostart") { var au = root.AddComponent<AutoStart>(); au.StartCoroutine(au.Run()); }
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-selftest") root.AddComponent<SelfTest>().StartCoroutine(root.GetComponent<SelfTest>().Run());
        }
    }
}
