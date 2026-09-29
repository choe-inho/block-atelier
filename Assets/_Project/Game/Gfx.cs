using UnityEngine;

namespace BlockAtelier.Game
{
    /// <summary>
    /// 코드로 만드는 스프라이트, 재질, 글꼴. 외부 에셋 없이 바로 돌아가게 한다.
    /// 나중에 디자이너 에셋이 생기면 여기만 바꾸면 된다.
    /// </summary>
    public static class Gfx
    {
        static Sprite block, well, pixel, circle, soft, ring, panel, gradient, streak, sparkle;
        static Material spriteMat, particleMat, trailMat;
        static Font font;

        /// <summary>광택 있는 블록 (흰색 바탕에 명암. SpriteRenderer.color로 색을 입힌다)</summary>
        public static Sprite Block { get { return block != null ? block : (block = MakeBlock(96)); } }
        /// <summary>빈칸용 평평한 둥근 사각형</summary>
        public static Sprite Well { get { return well != null ? well : (well = MakeRounded(64, 12, false)); } }
        public static Sprite Pixel { get { return pixel != null ? pixel : (pixel = MakeSquare()); } }
        public static Sprite Circle { get { return circle != null ? circle : (circle = MakeCircle(64, 0.9f)); } }
        /// <summary>가장자리가 부드러운 원 (빛, 방울 번짐)</summary>
        public static Sprite Soft { get { return soft != null ? soft : (soft = MakeCircle(64, 0.0f)); } }
        public static Sprite Ring { get { return ring != null ? ring : (ring = MakeRing(128)); } }
        /// <summary>9-슬라이스 패널 (버튼, 카드)</summary>
        public static Sprite Panel { get { return panel != null ? panel : (panel = MakeRounded(64, 22, true)); } }
        public static Sprite Gradient { get { return gradient != null ? gradient : (gradient = MakeGradient()); } }
        /// <summary>붓질 연출용 가로로 긴 빛줄기</summary>
        public static Sprite Streak { get { return streak != null ? streak : (streak = MakeStreak()); } }
        public static Sprite Sparkle { get { return sparkle != null ? sparkle : (sparkle = MakeSparkle(64)); } }

        public static Material SpriteMat
        {
            get
            {
                if (spriteMat == null)
                {
                    // 파이프라인 기본 스프라이트 재질을 빌려 온다 (URP 2D에서는 Sprite-Lit-Default).
                    var go = new GameObject("tmp");
                    spriteMat = go.AddComponent<SpriteRenderer>().sharedMaterial;
                    Object.Destroy(go);
                }
                return spriteMat;
            }
        }

        public static Material ParticleMat
        {
            get
            {
                if (particleMat == null)
                {
                    particleMat = new Material(SpriteMat) { name = "BA Particle" };
                    particleMat.mainTexture = Soft.texture;
                }
                return particleMat;
            }
        }

        public static Material ParticleSquareMat
        {
            get
            {
                if (trailMat == null)
                {
                    trailMat = new Material(SpriteMat) { name = "BA Chunk" };
                    trailMat.mainTexture = Block.texture;
                }
                return trailMat;
            }
        }

        public static Font UIFont
        {
            get
            {
                if (font == null)
                {
                    font = Font.CreateDynamicFontFromOSFont(new[]
                    {
                        "Apple SD Gothic Neo", "AppleSDGothicNeo-Bold", "Noto Sans CJK KR", "Noto Sans KR",
                        "NotoSansCJK-Regular", "Malgun Gothic", "Arial Unicode MS", "Arial"
                    }, 64);
                }
                return font;
            }
        }

        public static Color Hex(string hex)
        {
            Color c;
            if (hex == "transparent") return Color.clear;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.magenta;
        }

        // ---------------- 텍스처 생성 ----------------

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        static float RoundedMask(float x, float y, float size, float r)
        {
            // 가장자리까지의 부호 거리로 안티앨리어싱
            float cx = Mathf.Clamp(x, r, size - r), cy = Mathf.Clamp(y, r, size - r);
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return Mathf.Clamp01(r - d + 0.5f);
        }

        static Sprite MakeRounded(int size, int radius, bool sliced)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = RoundedMask(x + 0.5f, y + 0.5f, size, radius);
                    px[y * size + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels32(px);
            t.Apply();
            var border = sliced ? new Vector4(radius + 2, radius + 2, radius + 2, radius + 2) : Vector4.zero;
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, border);
        }

        static Sprite MakeBlock(int size)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            float r = size * 0.18f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float a = RoundedMask(fx, fy, size, r);
                    float v = fy / size;                       // 0 아래, 1 위
                    float shade = Mathf.Lerp(0.80f, 1.0f, v);  // 위가 밝게
                    // 아래 테두리 어둡게, 위 테두리 밝게 (살짝 도톰한 느낌)
                    float inner = RoundedMask(fx, fy + size * 0.07f, size, r);
                    if (inner < 0.5f && fy < size * 0.5f) shade *= 0.72f;
                    float top = RoundedMask(fx, fy - size * 0.06f, size, r);
                    if (top < 0.5f && fy > size * 0.5f) shade = Mathf.Min(1f, shade + 0.18f);
                    // 왼쪽 위 반짝임
                    float gx = (fx - size * 0.30f) / (size * 0.16f), gy = (fy - size * 0.74f) / (size * 0.08f);
                    float gloss = Mathf.Clamp01(1f - (gx * gx + gy * gy));
                    float c = Mathf.Clamp01(shade + gloss * 0.35f);
                    px[y * size + x] = new Color(c, c, c, a);
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite MakeSquare()
        {
            var t = NewTex(4, 4);
            t.filterMode = FilterMode.Point;
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        static Sprite MakeCircle(int size, float hardness)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c)) / c;
                    float a;
                    if (hardness > 0) a = Mathf.Clamp01((1f - d) * c);            // 또렷한 원
                    else a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);              // 부드러운 빛
                    px[y * size + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite MakeRing(int size)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c)) / c;
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.12f);
                    px[y * size + x] = new Color(1, 1, 1, a * a);
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite MakeGradient()
        {
            var t = NewTex(4, 128);
            var px = new Color32[4 * 128];
            var top = Hex("#2A2340");
            var bottom = Hex("#141722");
            for (int y = 0; y < 128; y++)
            {
                Color col = Color.Lerp(bottom, top, y / 127f);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = col;
            }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 4, 128), new Vector2(0.5f, 0.5f), 4);
        }

        static Sprite MakeStreak()
        {
            int w = 128, h = 32;
            var t = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1), v = Mathf.Abs(y / (float)(h - 1) - 0.5f) * 2f;
                    float along = Mathf.Pow(u, 1.6f) * Mathf.Clamp01((1f - u) * 12f);   // 앞쪽이 밝고 끝에서 급히 사라짐
                    float a = along * Mathf.Pow(Mathf.Clamp01(1f - v), 1.5f);
                    px[y * w + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h);
        }

        static Sprite MakeSparkle(int size)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - c) / c, dy = Mathf.Abs(y + 0.5f - c) / c;
                    float star = Mathf.Clamp01(1f - (dx * 6f) * dy - dy * 0.15f) * Mathf.Clamp01(1f - dx) +
                                 Mathf.Clamp01(1f - (dy * 6f) * dx - dx * 0.15f) * Mathf.Clamp01(1f - dy);
                    float core = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2.2f), 2f);
                    px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(star * 0.9f + core));
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>레벨 그림을 작은 텍스처 한 장으로 (레벨 목록 썸네일)</summary>
        /// <summary>그림 픽셀 색: 명암 0 밝음, 1 기본, 2 어두움 (Tools/art/pixelart.py와 같은 공식)</summary>
        public static Color Shade(Color c, int tone, Color line)
        {
            if (tone == 0) return Color.Lerp(c, Color.white, 0.32f);
            if (tone == 2) return Color.Lerp(c, line, 0.3f);
            return c;
        }

        /// <summary>안 칠한 픽셀: 명암은 살리고 캔버스 쪽으로 옅게</summary>
        public static Color Silhouette(Color shaded, Color canvas)
        {
            return Color.Lerp(shaded, canvas, 0.7f);
        }

        public static Sprite PictureSprite(Core.LevelData lv, bool colored)
        {
            int w = lv.PictureWidth, h = lv.PictureHeight;
            var t = NewTex(w, h);
            t.filterMode = FilterMode.Point;
            var px = new Color32[w * h];
            var line = Hex(lv.LineColor);
            var canvas = Hex("#EFE9DC");
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color col;
                    if (lv.IsLine(x, y)) col = line;
                    else
                    {
                        int c = lv.PixelAt(x, y);
                        if (c == 0) col = Color.clear;
                        else
                        {
                            col = Shade(Hex(lv.Palette[c]), lv.ShadeAt(x, y), line);
                            if (!colored) col = Silhouette(col, canvas);
                        }
                    }
                    px[(h - 1 - y) * w + x] = col;
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        public static SpriteRenderer MakeSprite(string name, Transform parent, Sprite s, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
