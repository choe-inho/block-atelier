using System.Collections.Generic;
using BlockAtelier.Core;
using UnityEngine;

namespace BlockAtelier.Game
{
    /// <summary>칠해지는 픽셀 그림. 아직 안 칠한 픽셀은 연한 색으로 윤곽만 보인다.</summary>
    public sealed class PictureView
    {
        public readonly Transform Root;
        public float PixelSize { get; private set; }
        SpriteRenderer[] pixels;
        Color[] shaded;     // 픽셀마다 명암이 들어간 제 색
        Color lineColor;
        SpriteRenderer frame, shine;
        LevelData level;
        Color[] palette;
        int w, h;
        public readonly HashSet<int> Pending = new HashSet<int>();

        public PictureView(Transform parent)
        {
            Root = new GameObject("Picture").transform;
            Root.SetParent(parent, false);
        }

        public void Setup(LevelData lv, Color[] colors, float maxSide)
        {
            level = lv;
            palette = colors;
            w = lv.PictureWidth;
            h = lv.PictureHeight;
            for (int i = Root.childCount - 1; i >= 0; i--) Object.Destroy(Root.GetChild(i).gameObject);
            Pending.Clear();

            PixelSize = maxSide / Mathf.Max(w, h);
            float pad = 0.22f;
            frame = UI.Panel("Canvas", Root, Vector2.zero, new Vector2(w * PixelSize + pad * 2, h * PixelSize + pad * 2), UI.Canvas, 6);
            // 액자 그림자 대신 살짝 어두운 테두리
            var edge = UI.Panel("CanvasEdge", Root, new Vector2(0f, -0.06f), new Vector2(w * PixelSize + pad * 2 + 0.08f, h * PixelSize + pad * 2 + 0.08f), UI.CanvasEdge, 5);
            edge.color = new Color(0f, 0f, 0f, 0.35f);

            lineColor = Gfx.Hex(lv.LineColor);
            pixels = new SpriteRenderer[w * h];
            shaded = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (lv.IsLine(x, y))
                    {
                        // 윤곽선은 처음부터 그려져 있다 (색칠 공부의 선)
                        var ln = Gfx.MakeSprite("Ln", Root, Gfx.Pixel, lineColor, 7);
                        ln.transform.localPosition = PixelLocal(idx);
                        ln.transform.localScale = Vector3.one * PixelSize * 1.02f;
                        continue;
                    }
                    int c = lv.PixelAt(x, y);
                    if (c == 0) continue;
                    shaded[idx] = Gfx.Shade(colors[c], lv.ShadeAt(x, y), lineColor);
                    var sr = Gfx.MakeSprite("Px", Root, Gfx.Pixel, Color.white, 7);
                    sr.transform.localPosition = PixelLocal(idx);
                    sr.transform.localScale = Vector3.one * PixelSize * 1.02f;
                    pixels[idx] = sr;
                }

            shine = Gfx.MakeSprite("Shine", Root, Gfx.Soft, new Color(1f, 1f, 1f, 0f), 9);
            shine.transform.localScale = Vector3.one * (Mathf.Max(w, h) * PixelSize * 1.6f);
        }

        public Vector3 PixelLocal(int idx)
        {
            int x = idx % w, y = idx / w;
            return new Vector3((x - (w - 1) * 0.5f) * PixelSize, ((h - 1) * 0.5f - y) * PixelSize, 0f);
        }

        public Vector3 PixelWorld(int idx) { return Root.TransformPoint(PixelLocal(idx)); }

        public Bounds FrameBounds { get { return frame.bounds; } }

        public Color ColorOfPixel(int idx) { return shaded[idx]; }

        public void Render(PictureModel pic)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                var sr = pixels[i];
                if (sr == null) continue;
                bool on = pic.IsPaintedIndex(i) && !Pending.Contains(i);
                sr.color = on ? shaded[i] : Gfx.Silhouette(shaded[i], UI.Canvas);
                sr.transform.localScale = Vector3.one * PixelSize * 1.02f;
                sr.sortingOrder = 7;
            }
        }

        /// <summary>페인트 방울이 닿았을 때: 흰색으로 번쩍 → 제 색, 크게 튀었다가 제자리.</summary>
        public void Reveal(int idx, PictureModel pic)
        {
            Pending.Remove(idx);
            var sr = pixels[idx];
            if (sr == null) return;
            var target = shaded[idx];
            var t = sr.transform;
            float baseScale = PixelSize * 1.02f;
            sr.sortingOrder = 8;
            Tween.Run(0.32f, x =>
            {
                float s = Mathf.LerpUnclamped(1.9f, 1f, Ease.OutBack(x));
                t.localScale = Vector3.one * baseScale * s;
                sr.color = Color.Lerp(Color.white, target, Mathf.Clamp01(x * 2.2f));
            }, () => { sr.sortingOrder = 7; });
        }

        /// <summary>완성 연출: 줄마다 반짝이며 쓸고 지나가고, 그림 전체가 한 번 튄다.</summary>
        public float Celebrate()
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                var sr = pixels[i];
                if (sr == null) continue;
                int y = i / w, x = i % w;
                float delay = (x + y) * 0.35f / Mathf.Max(1, w / 10f) * 0.1f;
                var t = sr.transform;
                var baseColor = sr.color;
                float baseScale = PixelSize * 1.02f;
                Tween.Run(0.35f, k =>
                {
                    float b = Ease.Bump(k);
                    t.localScale = Vector3.one * baseScale * (1f + 0.35f * b);
                    sr.color = Color.Lerp(baseColor, Color.white, 0.55f * b);
                }, null, delay);
            }
            var root = Root;
            var start = root.localScale;
            Tween.Run(0.5f, k => root.localScale = start * (1f + 0.12f * Ease.Bump(k)), null, 0.35f);
            var sh = shine;
            Tween.Run(0.9f, k => sh.color = new Color(1f, 1f, 1f, 0.55f * Ease.Bump(k)), null, 0.2f);
            return 0.35f + (w + h) * 0.35f / Mathf.Max(1, w / 10f) * 0.1f;
        }
    }
}
