using UnityEngine;

namespace BlockAtelier.Game
{
    /// <summary>
    /// 코드로 만드는 스프라이트, 재질, 글꼴. 외부 에셋 없이 바로 돌아가게 한다.
    /// 나중에 디자이너 에셋이 생기면 여기만 바꾸면 된다.
    /// </summary>
    public static class Gfx
    {
        static Sprite block, well, pixel, circle, soft, ring, panel, gradient, streak, sparkle, star, lockIcon, gear;
        static Material spriteMat, particleMat, trailMat;
        static Font font, fontBold;

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
        /// <summary>둥근 모서리 별 (평가 별)</summary>
        public static Sprite Star { get { return star != null ? star : (star = MakeStar(128)); } }
        /// <summary>자물쇠 아이콘 (잠긴 레벨)</summary>
        public static Sprite Lock { get { return lockIcon != null ? lockIcon : (lockIcon = MakeLock(96)); } }
        /// <summary>톱니바퀴 아이콘 (설정)</summary>
        public static Sprite Gear { get { return gear != null ? gear : (gear = MakeGear(96)); } }
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

        /// <summary>본문 글꼴: Pretendard SemiBold (Resources/Fonts). 없으면 OS 한글 글꼴.</summary>
        public static Font UIFont
        {
            get
            {
                if (font == null) font = Resources.Load<Font>("Fonts/Pretendard-SemiBold") ?? OSFont();
                return font;
            }
        }

        /// <summary>제목·숫자용 굵은 글꼴: Pretendard ExtraBold</summary>
        public static Font UIFontBold
        {
            get
            {
                if (fontBold == null) fontBold = Resources.Load<Font>("Fonts/Pretendard-ExtraBold") ?? UIFont;
                return fontBold;
            }
        }

        static Font OSFont()
        {
            return Font.CreateDynamicFontFromOSFont(new[]
            {
                "Apple SD Gothic Neo", "AppleSDGothicNeo-Bold", "Noto Sans CJK KR", "Noto Sans KR",
                "NotoSansCJK-Regular", "Malgun Gothic", "Arial Unicode MS", "Arial"
            }, 64);
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

        static Vector2[] starPts;

        static bool InStar(float x, float y)
        {
            // 중심 (0,0), 바깥 반지름 1, 안쪽 0.5인 다섯 꼭지 별 (곧은 변)
            if (starPts == null)
            {
                starPts = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                    float r = i % 2 == 0 ? 1f : 0.5f;
                    starPts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                }
            }
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                var a = starPts[i]; var b = starPts[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        static Sprite MakeStar(int size)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int hit = 0;
                    float shade = 0f;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float u = ((x + (sx + 0.5f) / ss) / size - 0.5f) * 2.15f;
                            float v = ((y + (sy + 0.5f) / ss) / size - 0.46f) * 2.15f;
                            if (InStar(u, v)) { hit++; shade += v > 0.1f ? 1f : 0.82f; }
                        }
                    float a = hit / (float)(ss * ss);
                    float g = hit == 0 ? 1f : shade / hit;   // 위쪽 절반을 살짝 밝게
                    px[y * size + x] = new Color(g, g, g, a);
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite MakeLock(int size)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float u = (x + (sx + 0.5f) / ss) / size, v = (y + (sy + 0.5f) / ss) / size;
                            // 몸통: 둥근 사각형, 열쇠 구멍은 비움
                            bool body = u > 0.18f && u < 0.82f && v > 0.08f && v < 0.56f;
                            float kx = u - 0.5f, ky = v - 0.36f;
                            bool hole = (kx * kx + ky * ky < 0.0045f) || (Mathf.Abs(kx) < 0.028f && v > 0.2f && v < 0.36f);
                            // 고리: 위쪽 반원 띠
                            float rx = u - 0.5f, ry = v - 0.56f;
                            float d = Mathf.Sqrt(rx * rx + ry * ry);
                            bool shackle = ry >= -0.02f && d > 0.15f && d < 0.24f;
                            if ((body && !hole) || shackle) hit++;
                        }
                    px[y * size + x] = new Color(1, 1, 1, hit / (float)(ss * ss));
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite MakeGear(int size)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float u = ((x + (sx + 0.5f) / ss) / size - 0.5f) * 2f, v = ((y + (sy + 0.5f) / ss) / size - 0.5f) * 2f;
                            float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                            // 톱니 8개: 각도에 따라 바깥 반지름이 0.72 ~ 0.92
                            float tooth = Mathf.Cos(a * 8f) > 0.25f ? 0.92f : 0.72f;
                            if (r <= tooth && r >= 0.3f) hit++;
                        }
                    px[y * size + x] = new Color(1, 1, 1, hit / (float)(ss * ss));
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
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

        static readonly System.Collections.Generic.Dictionary<string, Sprite> avatarCache = new System.Collections.Generic.Dictionary<string, Sprite>();

        /// <summary>픽셀 캐릭터 (24x24, 선명한 점). 같은 조합은 한 번만 만든다.</summary>
        public static Sprite AvatarSprite(Core.AvatarSpec spec)
        {
            string key = spec.Code;
            Sprite s;
            if (avatarCache.TryGetValue(key, out s) && s != null) return s;
            var px = Core.AvatarArt.Render(spec);
            int n = Core.AvatarArt.Size;
            var t = NewTex(n, n);
            t.filterMode = FilterMode.Point;
            var cols = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    uint v = px[y * n + x];
                    cols[(n - 1 - y) * n + x] = new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, (byte)(v >> 24));
                }
            t.SetPixels32(cols);
            t.Apply();
            s = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            if (avatarCache.Count > 64) avatarCache.Clear();
            avatarCache[key] = s;
            return s;
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
