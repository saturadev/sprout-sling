using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SproutSling
{
    /// Portrait HUD built in code: luck panel, power meter, reveal texts, flash.
    public class SproutUI
    {
        public RectTransform canvas;
        public GameObject luckPanel, meter;
        public TMP_Text luckText, zoneText, perfectText, hintText, oddsText, titleText, plantText, fullText, bestText, popText;
        public RectTransform marker;
        public Image flash, zoneBar;
        public const float MeterHeight = 560f;

        static TMP_FontAsset font;
        static Material outlined;

        public static SproutUI Build(Transform parent)
        {
            font = Resources.Load<TMP_FontAsset>("LilitaOne SDF");
            outlined = Resources.Load<Material>("LilitaOne SDF Outline");
            var ui = new SproutUI();
            var go = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 10;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1080, 1920);
            s.matchWidthOrHeight = 0.5f;
            ui.canvas = (RectTransform)go.transform;
            var root = ui.canvas;

            // Game name, top left.
            var name = Label(root, "SPROUT SLING", 38, new Color(1f, 0.92f, 0.5f), new Vector2(0, 1), new Vector2(40, -60), TextAnchor.UpperLeft, 3);
            name.rectTransform.pivot = new Vector2(0, 1);

            // Luck panel, top centre.
            ui.luckPanel = Panel(root, new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(460, 200), new Color(0.12f, 0.1f, 0.22f, 0.55f)).gameObject;
            var lp = ui.luckPanel.transform;
            ui.luckText = Label(lp, "x1", 104, new Color(1f, 0.84f, 0.2f), new Vector2(0.5f, 0.5f), new Vector2(0, 22), TextAnchor.MiddleCenter, 5);
            Label(lp, "LUCK", 36, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, -62), TextAnchor.MiddleCenter, 3);
            ui.zoneBar = Panel(lp, new Vector2(0.5f, 0), new Vector2(0, -50), new Vector2(560, 70), new Color(0.05f, 0.08f, 0.12f, 0.55f));
            ui.zoneText = Label(ui.zoneBar.transform, "Sunny Meadow", 44, new Color(0.55f, 1f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 3);

            // Power meter, right of centre (red bottom, green top).
            ui.meter = new GameObject("Meter", typeof(RectTransform));
            var m = (RectTransform)ui.meter.transform;
            m.SetParent(root, false);
            m.anchorMin = m.anchorMax = new Vector2(0.5f, 0.5f);
            m.anchoredPosition = new Vector2(190, 160);
            m.sizeDelta = new Vector2(120, MeterHeight);
            Panel(m, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132, MeterHeight + 26), new Color(1f, 1f, 1f, 0.95f));
            Panel(m, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(116, MeterHeight + 10), new Color(0.1f, 0.08f, 0.12f, 1f));
            const int segs = 14;
            for (int i = 0; i < segs; i++)
            {
                float t = i / (segs - 1f);
                Color col = t < 0.5f ? Color.Lerp(new Color(1f, 0.22f, 0.12f), new Color(1f, 0.8f, 0.1f), t * 2f) : Color.Lerp(new Color(1f, 0.8f, 0.1f), new Color(0.3f, 0.95f, 0.25f), (t - 0.5f) * 2f);
                float h = MeterHeight / segs;
                Panel(m, new Vector2(0.5f, 0f), new Vector2(0, h * (i + 0.5f)), new Vector2(100, h - 4), col);
            }
            // PERFECT band on top.
            var band = Panel(m, new Vector2(0.5f, 0f), new Vector2(0, MeterHeight * 0.95f), new Vector2(100, MeterHeight * 0.1f - 4), new Color(0.6f, 1f, 0.5f));
            band.gameObject.AddComponent<Outline>().effectColor = Color.white;
            ui.marker = Panel(m, new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(160, 26), Color.white).rectTransform;
            ui.marker.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3, -3);
            ui.perfectText = Label(m, "PERFECT!", 64, new Color(0.45f, 1f, 0.35f), new Vector2(0.5f, 0f), new Vector2(0, -80), TextAnchor.MiddleCenter, 5);
            ui.perfectText.gameObject.SetActive(false);

            ui.hintText = Label(root, "TAP TO THROW", 56, Color.white, new Vector2(0.5f, 0), new Vector2(0, 330), TextAnchor.MiddleCenter, 4);

            // Reveal texts.
            ui.plantText = Label(root, "", 48, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 560), TextAnchor.MiddleCenter, 4);
            ui.titleText = Label(root, "", 150, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 430), TextAnchor.MiddleCenter, 7);
            ui.oddsText = Label(root, "", 96, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 40), TextAnchor.MiddleCenter, 6);
            ui.fullText = Label(root, "", 46, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 330), TextAnchor.MiddleCenter, 4);
            ui.popText = Label(root, "", 70, new Color(0.45f, 1f, 0.4f), new Vector2(0.5f, 1), new Vector2(0, -580), TextAnchor.MiddleCenter, 5);
            ui.bestText = Label(root, "", 40, new Color(1f, 1f, 1f, 0.9f), new Vector2(0.5f, 0), new Vector2(0, 150), TextAnchor.MiddleCenter, 3);

            ui.flash = Panel(root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0));
            ui.flash.rectTransform.anchorMin = Vector2.zero;
            ui.flash.rectTransform.anchorMax = Vector2.one;
            ui.flash.raycastTarget = false;
            return ui;
        }

        public void SetMeter(float v) => marker.anchoredPosition = new Vector2(0, Mathf.Clamp01(v) * MeterHeight);

        static Image Panel(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var g = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)g.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = g.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static TMP_Text Label(Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 pos, TextAnchor align, float outline = 0)
        {
            var g = new GameObject("Text", typeof(RectTransform));
            var rt = (RectTransform)g.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1000, size * 1.3f);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.font = font;
            if (outline > 0 && outlined) t.fontSharedMaterial = outlined;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align == TextAnchor.UpperLeft ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
            t.raycastTarget = false;
            return t;
        }
    }
}
