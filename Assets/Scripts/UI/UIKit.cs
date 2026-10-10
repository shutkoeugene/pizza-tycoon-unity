using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PizzaTycoon
{
    public static class UIKit
    {
        public static Font Font;
        public static Sprite Round, RoundSmall;
        public static readonly Color Panel = new Color(0.09f, 0.10f, 0.14f, 0.92f);
        public static readonly Color PanelLight = new Color(0.16f, 0.18f, 0.24f, 0.95f);
        public static readonly Color Accent = new Color(0.96f, 0.62f, 0.12f);
        public static readonly Color Good = new Color(0.35f, 0.78f, 0.4f);
        public static readonly Color Bad = new Color(0.92f, 0.32f, 0.3f);
        public static readonly Color TextCol = new Color(0.94f, 0.94f, 0.96f);
        public static readonly Color Dim = new Color(0.65f, 0.68f, 0.75f);

        public static void Init()
        {
            if (Font == null)
            {
                Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (Font == null) Font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            if (Round == null) Round = MakeRounded(48, 14, 16);
            if (RoundSmall == null) RoundSmall = MakeRounded(24, 7, 8);
        }

        static Sprite MakeRounded(int size, int radius, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - dist + 0.5f);
                    px[y * size + x] = new Color(1, 1, 1, a);
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        public static Canvas CreateCanvas()
        {
            var go = new GameObject("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        public static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        public static Image Box(Transform parent, Color c, string name = "Box", bool small = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = small ? RoundSmall : Round; img.type = Image.Type.Sliced; img.color = c;
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 20, Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color ?? TextCol;
            t.alignment = anchor; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static LayoutElement Size(GameObject go, float w = -1, float h = -1, float flexW = -1)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = w;
            if (h >= 0) le.preferredHeight = h;
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        public static Button Btn(Transform parent, string label, Action onClick, Color? color = null, int fontSize = 20, float w = -1, float h = 44)
        {
            var img = Box(parent, color ?? PanelLight, "Button", true);
            var b = img.gameObject.AddComponent<Button>();
            var cs = b.colors;
            cs.normalColor = Color.white; cs.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cs.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            cs.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            b.colors = cs; b.targetGraphic = img;
            var t = Label(img.transform, label, fontSize, TextCol, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.rectTransform, 8, 8, 2, 2);
            Size(img.gameObject, w, h);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        public static RectTransform VBox(Transform parent, float spacing = 8, int pad = 0, bool fitHeight = false)
        {
            var rt = Rect("VBox", parent);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            if (fitHeight) { var f = rt.gameObject.AddComponent<ContentSizeFitter>(); f.verticalFit = ContentSizeFitter.FitMode.PreferredSize; }
            return rt;
        }

        public static RectTransform HBox(Transform parent, float spacing = 8, float height = -1, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rt = Rect("HBox", parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.childAlignment = align;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            if (height > 0) Size(rt.gameObject, -1, height);
            return rt;
        }

        public static RectTransform Scroll(Transform parent, out RectTransform content)
        {
            var rt = Rect("Scroll", parent);
            var sr = rt.gameObject.AddComponent<ScrollRect>();
            var bg = rt.gameObject.AddComponent<Image>(); bg.color = new Color(0, 0, 0, 0.001f);
            var vp = Rect("Viewport", rt);
            Stretch(vp);
            vp.gameObject.AddComponent<RectMask2D>();
            vp.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            content = VBox(vp, 8, 4, true);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = new Vector2(0, 0); content.offsetMax = new Vector2(0, 0);
            sr.viewport = vp; sr.content = content; sr.horizontal = false; sr.vertical = true;
            sr.scrollSensitivity = 30f; sr.movementType = ScrollRect.MovementType.Clamped;
            return rt;
        }

        public static Text Row(Transform parent, string left, string right, Color? rightColor = null, int size = 20)
        {
            var h = HBox(parent, 6, 30);
            var l = Label(h, left, size, Dim); Size(l.gameObject, -1, -1, 1);
            var r = Label(h, right, size, rightColor ?? TextCol, TextAnchor.MiddleRight, FontStyle.Bold); Size(r.gameObject, 180, -1);
            return r;
        }

        public static string Money(float v) => (v < 0 ? "-" : "") + "$" + Mathf.Abs(Mathf.RoundToInt(v)).ToString("N0");
    }
}
