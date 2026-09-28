using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAtelier.Game
{
    /// <summary>화면 색과 글자, 버튼 만들기. 모든 UI는 월드 공간에 둔다 (캡처와 레이아웃이 단순해진다).</summary>
    public static class UI
    {
        public static readonly Color Ink = Gfx.Hex("#E6E9F2");
        public static readonly Color Muted = Gfx.Hex("#8E97B3");
        public static readonly Color Accent = Gfx.Hex("#FFD166");
        public static readonly Color Hard = Gfx.Hex("#FF6B6B");
        public static readonly Color Good = Gfx.Hex("#6EE7A8");
        public static readonly Color BoardPanel = Gfx.Hex("#1F2336");
        public static readonly Color Well = Gfx.Hex("#2B3049");
        public static readonly Color Button = Gfx.Hex("#2E3350");
        public static readonly Color ButtonPri = Gfx.Hex("#FFD166");
        public static readonly Color ButtonPriInk = Gfx.Hex("#2A2340");
        public static readonly Color Canvas = Gfx.Hex("#EFE9DC");
        public static readonly Color CanvasEdge = Gfx.Hex("#C9BEA6");
        public static readonly Color GrayBlock = Gfx.Hex("#767D92");
        public static readonly Color Dim = new Color(0.04f, 0.04f, 0.08f, 0.72f);

        public const float CanvasScale = 0.01f; // 캔버스 100단위 = 월드 1

        public static Canvas WorldCanvas(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = order;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(1000f, 1000f);
            rt.localScale = Vector3.one * CanvasScale;
            rt.localPosition = Vector3.zero;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2f;
            return canvas;
        }

        /// <summary>글자 하나. pos와 size는 월드 단위 (캔버스 부모 기준).</summary>
        public static Text Label(Canvas canvas, string text, Vector2 pos, float size, Color color,
                                 TextAnchor align = TextAnchor.MiddleCenter, float width = 8f, bool bold = false)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var t = go.AddComponent<Text>();
            t.font = Gfx.UIFont;
            t.text = text;
            t.fontSize = Mathf.RoundToInt(size * 100f);
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(width * 100f, size * 140f);
            rt.pivot = PivotFor(align);
            rt.anchoredPosition = pos * 100f;
            return t;
        }

        static Vector2 PivotFor(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.MiddleLeft: case TextAnchor.UpperLeft: case TextAnchor.LowerLeft: return new Vector2(0f, 0.5f);
                case TextAnchor.MiddleRight: case TextAnchor.UpperRight: case TextAnchor.LowerRight: return new Vector2(1f, 0.5f);
                default: return new Vector2(0.5f, 0.5f);
            }
        }

        public static SpriteRenderer Panel(string name, Transform parent, Vector2 pos, Vector2 size, Color color, int order)
        {
            var sr = Gfx.MakeSprite(name, parent, Gfx.Panel, color, order);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.transform.localPosition = pos;
            return sr;
        }
    }

    /// <summary>월드 공간 버튼. 눌림 판정은 GameRoot가 직접 한다.</summary>
    public sealed class WorldButton
    {
        public readonly SpriteRenderer Bg;
        public readonly Text Label;
        public Action OnClick;
        public bool Enabled = true;
        public bool Visible = true;
        Color baseColor;

        public WorldButton(Transform parent, Canvas canvas, string text, Vector2 pos, Vector2 size, bool primary, int order)
        {
            baseColor = primary ? UI.ButtonPri : UI.Button;
            Bg = UI.Panel("Button", parent, pos, size, baseColor, order);
            Label = UI.Label(canvas, text, pos, Mathf.Min(0.4f, size.y * 0.42f), primary ? UI.ButtonPriInk : UI.Ink, TextAnchor.MiddleCenter, size.x, true);
        }

        public bool Hit(Vector3 world)
        {
            if (!Visible || !Enabled || Bg == null) return false;
            var b = Bg.bounds;
            return world.x >= b.min.x && world.x <= b.max.x && world.y >= b.min.y && world.y <= b.max.y;
        }

        public void SetEnabled(bool on)
        {
            Enabled = on;
            if (Bg == null) return;
            var c = baseColor; c.a = on ? 1f : 0.45f;
            Bg.color = c;
            var lc = Label.color; lc.a = on ? 1f : 0.45f;
            Label.color = lc;
        }

        public void SetVisible(bool on)
        {
            Visible = on;
            if (Bg != null) Bg.gameObject.SetActive(on);
            if (Label != null) Label.gameObject.SetActive(on);
        }

        public void Press()
        {
            if (Bg == null) return;
            var t = Bg.transform;
            Tween.Run(0.18f, x => t.localScale = Vector3.one * (1f - 0.08f * Ease.Bump(x)));
            Sfx.Play(Sfx.Tick, 0.6f);
        }
    }
}
