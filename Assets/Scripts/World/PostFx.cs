using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PizzaTycoon
{
    public static class PostFx
    {
        public static void Setup(Camera cam)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data == null) return;
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;

            var go = new GameObject("PostFX");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = prof;
            var bloom = prof.Add<Bloom>(true);
            bloom.intensity.Override(0.55f); bloom.threshold.Override(1.0f); bloom.scatter.Override(0.6f);
            var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.28f); vig.smoothness.Override(0.4f);
            var ca = prof.Add<ColorAdjustments>(true);
            ca.saturation.Override(14f); ca.contrast.Override(10f); ca.postExposure.Override(0.15f);
            var sm = prof.Add<ShadowsMidtonesHighlights>(true);
            sm.shadows.Override(new Vector4(0.9f, 0.95f, 1.1f, 0f));
            sm.highlights.Override(new Vector4(1.08f, 1.02f, 0.92f, 0f));
        }
    }
}
