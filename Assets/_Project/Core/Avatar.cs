using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BlockAtelier.Core
{
    /// <summary>
    /// 픽셀 사람 캐릭터(상반신) 조합. 부위마다 번호 하나씩, 문자열로 저장한다 ("s1h3c0e2m0o1p4a0").
    /// 그림과 같은 방식(벡터 도형 → 24x24, 자동 명암·윤곽선)으로 그려서 그림 앨범과 한 세트처럼 보인다.
    /// </summary>
    public struct AvatarSpec
    {
        public int Skin, Hair, HairColor, Eyes, Mouth, Outfit, OutfitColor, Accessory;

        public static readonly string[] SkinColors = { "#FFE3CC", "#F6CBA5", "#E3A77B", "#C08159", "#8D5A3B" };
        public static readonly string[] HairColors = { "#3A3346", "#6B4630", "#A0673F", "#E2B35F", "#E05A47", "#F29BB1", "#5DA9E9", "#9B7FD4", "#DCDDE6" };
        public static readonly string[] OutfitColors = { "#5DA9E9", "#E05A47", "#81B29A", "#F6C85F", "#9B7FD4", "#F7A8B8", "#474556", "#F4F1EA" };
        public static readonly string[] HairNames = { "짧은 머리", "단발", "긴 머리", "올림머리", "삐죽 머리", "양갈래" };
        public static readonly string[] EyeNames = { "동그란 눈", "웃는 눈", "반짝 눈", "졸린 눈" };
        public static readonly string[] MouthNames = { "미소", "활짝", "앙" };
        public static readonly string[] OutfitNames = { "티셔츠", "셔츠", "후드티" };
        public static readonly string[] AccessoryNames = { "없음", "선글라스", "리본", "헤드폰", "비니", "꽃핀" };

        public const string Keys = "shcemopa";

        public int PartCount(int part)
        {
            switch (part)
            {
                case 0: return SkinColors.Length;
                case 1: return HairNames.Length;
                case 2: return HairColors.Length;
                case 3: return EyeNames.Length;
                case 4: return MouthNames.Length;
                case 5: return OutfitNames.Length;
                case 6: return OutfitColors.Length;
                default: return AccessoryNames.Length;
            }
        }

        public int Get(int part)
        {
            switch (part)
            {
                case 0: return Skin; case 1: return Hair; case 2: return HairColor; case 3: return Eyes;
                case 4: return Mouth; case 5: return Outfit; case 6: return OutfitColor; default: return Accessory;
            }
        }

        public AvatarSpec With(int part, int value)
        {
            var s = this;
            int n = PartCount(part);
            value = ((value % n) + n) % n;
            switch (part)
            {
                case 0: s.Skin = value; break; case 1: s.Hair = value; break; case 2: s.HairColor = value; break;
                case 3: s.Eyes = value; break; case 4: s.Mouth = value; break; case 5: s.Outfit = value; break;
                case 6: s.OutfitColor = value; break; default: s.Accessory = value; break;
            }
            return s;
        }

        public static AvatarSpec Random(int seed)
        {
            var rng = new System.Random(seed);
            var s = new AvatarSpec();
            for (int p = 0; p < Keys.Length; p++) s = s.With(p, rng.Next(s.PartCount(p)));
            if (rng.NextDouble() < 0.5) s.Accessory = 0;   // 절반은 장식 없이
            return s;
        }

        public string Code
        {
            get
            {
                var sb = new StringBuilder();
                for (int p = 0; p < Keys.Length; p++) sb.Append(Keys[p]).Append(Get(p).ToString(CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        /// <summary>잘못된 문자열은 null. 범위를 벗어난 번호는 범위 안으로 접는다.</summary>
        public static AvatarSpec? Parse(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            var s = new AvatarSpec();
            int i = 0, found = 0;
            while (i < code.Length)
            {
                int part = Keys.IndexOf(code[i]);
                if (part < 0) return null;
                int j = i + 1;
                while (j < code.Length && char.IsDigit(code[j])) j++;
                int v;
                if (j == i + 1 || !int.TryParse(code.Substring(i + 1, j - i - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return null;
                s = s.With(part, v);
                found++;
                i = j;
            }
            return found == Keys.Length ? s : (AvatarSpec?)null;
        }
    }

    /// <summary>캐릭터를 픽셀로 그린다. 결과는 위에서부터 한 줄씩, 0xAARRGGBB.</summary>
    public static class AvatarArt
    {
        public const int Size = 24;
        const uint LineColor = 0xFF2B2838;
        const uint Ink = 0xFF2B2838;

        enum Kind { Ellipse, RoundRect, Poly, Capsule, Arc }
        enum ShadeMode { Sphere, Box, Flat }

        sealed class Shape
        {
            public Kind K;
            public float[] P;
            public float[] Pts;
            public uint Color;
            public ShadeMode Mode;
            public int Tone = -1;
            public bool IsInk;
            public bool Erase;

            public bool Contains(float x, float y)
            {
                switch (K)
                {
                    case Kind.Ellipse:
                    {
                        float dx = (x - P[0]) / P[2], dy = (y - P[1]) / P[3];
                        return dx * dx + dy * dy <= 1f;
                    }
                    case Kind.RoundRect:
                    {
                        float x0 = P[0], y0 = P[1], x1 = P[2], y1 = P[3], r = P[4];
                        if (x < x0 || x > x1 || y < y0 || y > y1) return false;
                        float cx = Math.Min(Math.Max(x, x0 + r), x1 - r), cy = Math.Min(Math.Max(y, y0 + r), y1 - r);
                        return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
                    }
                    case Kind.Poly:
                    {
                        bool inside = false;
                        int n = Pts.Length / 2;
                        for (int i = 0, j = n - 1; i < n; j = i++)
                        {
                            float xi = Pts[i * 2], yi = Pts[i * 2 + 1], xj = Pts[j * 2], yj = Pts[j * 2 + 1];
                            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi + 1e-9f) + xi) inside = !inside;
                        }
                        return inside;
                    }
                    case Kind.Capsule:
                    {
                        float vx = P[2] - P[0], vy = P[3] - P[1], l2 = vx * vx + vy * vy;
                        float t = l2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((x - P[0]) * vx + (y - P[1]) * vy) / l2));
                        float px = P[0] + t * vx - x, py = P[1] + t * vy - y;
                        return px * px + py * py <= P[4] * P[4] / 4f;
                    }
                    default:
                    {
                        float d = (float)Math.Sqrt((x - P[0]) * (x - P[0]) + (y - P[1]) * (y - P[1]));
                        if (Math.Abs(d - P[2]) > P[3] / 2f) return false;
                        float a = (float)(Math.Atan2(y - P[1], x - P[0]) * 180.0 / Math.PI);
                        a = ((a % 360f) + 360f) % 360f;
                        float a0 = ((P[4] % 360f) + 360f) % 360f, a1 = ((P[5] % 360f) + 360f) % 360f;
                        return a0 <= a1 ? (a >= a0 && a <= a1) : (a >= a0 || a <= a1);
                    }
                }
            }

            void Bounds(out float x0, out float y0, out float x1, out float y1)
            {
                switch (K)
                {
                    case Kind.Ellipse: x0 = P[0] - P[2]; x1 = P[0] + P[2]; y0 = P[1] - P[3]; y1 = P[1] + P[3]; return;
                    case Kind.RoundRect: x0 = P[0]; y0 = P[1]; x1 = P[2]; y1 = P[3]; return;
                    case Kind.Poly:
                        x0 = y0 = float.MaxValue; x1 = y1 = float.MinValue;
                        for (int i = 0; i < Pts.Length; i += 2)
                        {
                            x0 = Math.Min(x0, Pts[i]); x1 = Math.Max(x1, Pts[i]);
                            y0 = Math.Min(y0, Pts[i + 1]); y1 = Math.Max(y1, Pts[i + 1]);
                        }
                        return;
                    default: x0 = 0; y0 = 0; x1 = 100; y1 = 100; return;
                }
            }

            public int ToneAt(float x, float y)
            {
                if (Tone >= 0) return Tone;
                if (Mode == ShadeMode.Flat) return 1;
                float x0, y0, x1, y1;
                Bounds(out x0, out y0, out x1, out y1);
                float w = Math.Max(x1 - x0, 1e-3f), h = Math.Max(y1 - y0, 1e-3f);
                if (Mode == ShadeMode.Sphere)
                {
                    float nx = (x - (x0 + x1) / 2f) / (w / 2f), ny = (y - (y0 + y1) / 2f) / (h / 2f);
                    float light = -(nx * 0.62f + ny * 0.78f);
                    return light > 0.5f ? 0 : light < -0.66f ? 2 : 1;
                }
                float u = (x - x0) / w, v = (y - y0) / h, t = u * 0.4f + v * 0.6f;
                return t < 0.16f ? 0 : t > 0.88f ? 2 : 1;
            }
        }

        sealed class Canvas
        {
            public readonly List<Shape> Shapes = new List<Shape>();
            Shape Add(Shape s) { Shapes.Add(s); return s; }
            public Shape Ellipse(float cx, float cy, float rx, float ry, uint c, ShadeMode m = ShadeMode.Sphere)
            { return Add(new Shape { K = Kind.Ellipse, P = new[] { cx, cy, rx, ry }, Color = c, Mode = m }); }
            public Shape Rect(float x0, float y0, float x1, float y1, float r, uint c, ShadeMode m = ShadeMode.Box)
            { return Add(new Shape { K = Kind.RoundRect, P = new[] { x0, y0, x1, y1, r }, Color = c, Mode = m }); }
            public Shape Poly(float[] pts, uint c, ShadeMode m = ShadeMode.Box)
            { return Add(new Shape { K = Kind.Poly, Pts = pts, Color = c, Mode = m }); }
            public Shape Cap(float x0, float y0, float x1, float y1, float w, uint c)
            { return Add(new Shape { K = Kind.Capsule, P = new[] { x0, y0, x1, y1, w }, Color = c, Mode = ShadeMode.Flat }); }
            public Shape Arc(float cx, float cy, float r, float w, float a0, float a1, uint c)
            { return Add(new Shape { K = Kind.Arc, P = new[] { cx, cy, r, w, a0, a1 }, Color = c, Mode = ShadeMode.Flat }); }
            public Shape InkDot(float cx, float cy, float r) { var s = Ellipse(cx, cy, r, r, Ink, ShadeMode.Flat); s.IsInk = true; return s; }
            public Shape InkCap(float x0, float y0, float x1, float y1, float w) { var s = Cap(x0, y0, x1, y1, w, Ink); s.IsInk = true; return s; }
            public Shape InkArc(float cx, float cy, float r, float w, float a0, float a1) { var s = Arc(cx, cy, r, w, a0, a1, Ink); s.IsInk = true; return s; }
        }

        static uint Hex(string h)
        {
            return 0xFF000000u | uint.Parse(h.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        /// <summary>반타원(위쪽) 윤곽 + 아래 점들로 머리 덮개 다각형을 만든다.</summary>
        static float[] CapPoly(float cx, float cy, float rx, float ry, float[] bottom)
        {
            var pts = new List<float>();
            for (int a = 180; a <= 360; a += 10)
            {
                double r = a * Math.PI / 180.0;
                pts.Add(cx + rx * (float)Math.Cos(r));
                pts.Add(cy + ry * (float)Math.Sin(r));
            }
            pts.AddRange(bottom);
            return pts.ToArray();
        }

        static Canvas Compose(AvatarSpec s)
        {
            var c = new Canvas();
            uint skin = Hex(AvatarSpec.SkinColors[s.Skin]);
            uint hair = Hex(AvatarSpec.HairColors[s.HairColor]);
            uint cloth = Hex(AvatarSpec.OutfitColors[s.OutfitColor]);
            uint white = Hex("#F4F1EA"), blush = Hex("#F29BB1"), mouthRed = Hex("#C2464A");
            float hx = 50, hy = 46, hrx = 25, hry = 24;          // 머리

            // 뒤쪽 머리카락 (긴 머리, 단발은 머리 뒤로 늘어진다)
            if (s.Hair == 2) c.Rect(22, 34, 78, 90, 14, hair);
            if (s.Hair == 1) c.Rect(23, 34, 77, 70, 12, hair);
            if (s.Hair == 5) { c.Ellipse(17, 62, 8, 14, hair); c.Ellipse(83, 62, 8, 14, hair); }
            if (s.Hair == 3) c.Ellipse(50, 17, 11, 10, hair);

            // 몸, 목
            if (s.Outfit == 2) c.Ellipse(50, 78, 22, 9, cloth).Tone = 2;          // 후드 모자 (목 뒤)
            c.Rect(10, 76, 90, 118, 22, cloth);
            c.Rect(45, 64, 55, 78, 3, skin, ShadeMode.Flat).Tone = 2;
            if (s.Outfit == 0) c.Ellipse(50, 77, 6, 3, skin, ShadeMode.Flat).Tone = 2;   // 둥근 목선
            if (s.Outfit == 1)
            {
                c.Poly(new float[] { 50, 84, 36, 74, 45, 72 }, white, ShadeMode.Flat);
                c.Poly(new float[] { 50, 84, 64, 74, 55, 72 }, white, ShadeMode.Flat);
            }
            if (s.Outfit == 2)
            {
                c.Cap(45, 80, 44, 94, 2.5f, white);
                c.Cap(55, 80, 56, 94, 2.5f, white);
            }

            // 귀, 머리
            c.Ellipse(26, 50, 6, 7, skin);
            c.Ellipse(74, 50, 6, 7, skin);
            c.Ellipse(hx, hy, hrx, hry, skin);

            // 볼
            c.Ellipse(33, 58, 5, 3, blush, ShadeMode.Flat);
            c.Ellipse(67, 58, 5, 3, blush, ShadeMode.Flat);

            // 눈
            float ey = 50, exl = 39, exr = 61;
            switch (s.Eyes)
            {
                case 0: c.InkDot(exl, ey, 3.3f); c.InkDot(exr, ey, 3.3f); break;
                case 1: c.InkArc(exl, ey + 3, 4.5f, 2.6f, 200, 340); c.InkArc(exr, ey + 3, 4.5f, 2.6f, 200, 340); break;
                case 2:
                    c.Ellipse(exl, ey, 4, 5, Ink, ShadeMode.Flat).IsInk = true;
                    c.Ellipse(exr, ey, 4, 5, Ink, ShadeMode.Flat).IsInk = true;
                    c.Ellipse(exl - 1.2f, ey - 1.6f, 1.6f, 1.6f, white, ShadeMode.Flat);
                    c.Ellipse(exr - 1.2f, ey - 1.6f, 1.6f, 1.6f, white, ShadeMode.Flat);
                    break;
                default: c.InkCap(exl - 4, ey + 1, exl + 4, ey + 1, 2.6f); c.InkCap(exr - 4, ey + 1, exr + 4, ey + 1, 2.6f); break;
            }

            // 입
            switch (s.Mouth)
            {
                case 0: c.InkArc(50, 57, 4.5f, 2.6f, 20, 160); break;
                case 1: c.Ellipse(50, 61, 4.5f, 3.2f, mouthRed, ShadeMode.Flat).IsInk = true; break;
                default: c.InkCap(47, 61, 53, 61, 2.6f); break;
            }

            // 앞머리
            switch (s.Hair)
            {
                case 0:   // 짧은 머리: 일자 앞머리 + 옆머리
                    c.Poly(CapPoly(hx, hy, hrx + 2, hry + 3, new float[] { 76, 50, 72, 38, 58, 37, 50, 40, 40, 36, 28, 38, 24, 50 }), hair, ShadeMode.Sphere);
                    break;
                case 1:   // 단발: 옆으로 둥글게 내려옴
                    c.Poly(CapPoly(hx, hy, hrx + 3, hry + 3, new float[] { 78, 64, 72, 64, 70, 40, 56, 38, 50, 42, 44, 38, 30, 40, 28, 64, 22, 64 }), hair, ShadeMode.Sphere);
                    break;
                case 2:   // 긴 머리: 가르마 + 양옆으로 길게
                    c.Poly(CapPoly(hx, hy, hrx + 3, hry + 3, new float[] { 78, 86, 71, 86, 70, 42, 52, 34, 48, 34, 30, 42, 29, 86, 22, 86 }), hair, ShadeMode.Sphere);
                    break;
                case 3:   // 올림머리: 이마가 드러나게 뒤로
                    c.Poly(CapPoly(hx, hy, hrx + 2, hry + 2, new float[] { 76, 46, 70, 34, 50, 29, 30, 34, 24, 46 }), hair, ShadeMode.Sphere);
                    break;
                case 4:   // 삐죽 머리
                    c.Poly(new float[] { 22, 48, 20, 30, 28, 32, 30, 16, 40, 24, 48, 10, 56, 22, 68, 12, 70, 28, 80, 26, 78, 48, 72, 38, 60, 40, 50, 36, 40, 40, 28, 38 }, hair, ShadeMode.Sphere);
                    break;
                default:  // 양갈래: 앞머리 + 옆 묶음(뒤에 그림)
                    c.Poly(CapPoly(hx, hy, hrx + 2, hry + 3, new float[] { 76, 48, 70, 38, 60, 40, 50, 36, 40, 40, 30, 38, 24, 48 }), hair, ShadeMode.Sphere);
                    break;
            }

            // 장식
            switch (s.Accessory)
            {
                case 1:   // 선글라스
                    c.InkArc(exl, ey, 7f, 3.4f, 0, 360); c.InkArc(exr, ey, 7f, 3.4f, 0, 360); c.InkCap(44, ey - 1, 56, ey - 1, 3f);
                    break;
                case 2:   // 리본
                {
                    uint rb = Hex("#E05A77");
                    c.Poly(new float[] { 66, 26, 56, 18, 56, 34 }, rb, ShadeMode.Flat);
                    c.Poly(new float[] { 66, 26, 76, 18, 76, 34 }, rb, ShadeMode.Flat);
                    c.Ellipse(66, 26, 3.5f, 3.5f, rb, ShadeMode.Flat).Tone = 2;
                    break;
                }
                case 3:   // 헤드폰
                {
                    uint hp = Hex("#474556");
                    c.Arc(50, 46, 29, 5, 190, 350, hp);
                    c.Rect(18, 42, 28, 60, 4, hp);
                    c.Rect(72, 42, 82, 60, 4, hp);
                    break;
                }
                case 4:   // 비니
                {
                    uint bn = Hex("#E05A47");
                    c.Poly(CapPoly(hx, 40, hrx + 3, hry + 2, new float[] { 78, 40, 22, 40 }), bn, ShadeMode.Sphere);
                    c.Rect(21, 34, 79, 42, 4, bn).Tone = 2;
                    c.Ellipse(50, 13, 6, 6, white);
                    break;
                }
                case 5:   // 꽃핀
                {
                    uint pk = Hex("#F7C948");
                    for (int i = 0; i < 5; i++)
                    {
                        double a = (-90 + i * 72) * Math.PI / 180.0;
                        c.Ellipse(30 + 5 * (float)Math.Cos(a), 30 + 5 * (float)Math.Sin(a), 4, 4, Hex("#F7A8B8"), ShadeMode.Flat);
                    }
                    c.Ellipse(30, 30, 3, 3, pk, ShadeMode.Flat);
                    break;
                }
            }
            return c;
        }

        /// <summary>24x24 픽셀. 위쪽 줄부터, 0xAARRGGBB (0 = 투명).</summary>
        public static uint[] Render(AvatarSpec spec)
        {
            var canvas = Compose(spec);
            var shapes = canvas.Shapes;
            int n = Size;
            const int ss = 5;
            float cell = 100f / n;
            var outp = new uint[n * n];
            var filled = new bool[n * n];
            var votes = new Dictionary<uint, int>();
            for (int py = 0; py < n; py++)
                for (int px = 0; px < n; px++)
                {
                    votes.Clear();
                    int empty = 0, ink = 0;
                    Shape inkShape = null;
                    var firstShape = new Dictionary<uint, Shape>();
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float x = (px + (sx + 0.5f) / ss) * cell, y = (py + (sy + 0.5f) / ss) * cell;
                            Shape hit = null;
                            for (int k = shapes.Count - 1; k >= 0; k--)
                                if (shapes[k].Contains(x, y)) { hit = shapes[k]; break; }
                            if (hit == null) { empty++; continue; }
                            if (hit.IsInk) { ink++; inkShape = hit; continue; }
                            int cnt;
                            votes.TryGetValue(hit.Color, out cnt);
                            votes[hit.Color] = cnt + 1;
                            if (!firstShape.ContainsKey(hit.Color)) firstShape[hit.Color] = hit;
                        }
                    int total = ss * ss;
                    int idx = py * n + px;
                    if (ink >= total * 0.3f) { outp[idx] = inkShape.Color; filled[idx] = true; continue; }
                    if (empty > total / 2 || votes.Count == 0) continue;
                    uint best = 0; int bestN = -1;
                    foreach (var kv in votes) if (kv.Value > bestN) { best = kv.Key; bestN = kv.Value; }
                    float cx = (px + 0.5f) * cell, cyy = (py + 0.5f) * cell;
                    Shape sh = null;
                    for (int k = shapes.Count - 1; k >= 0; k--)
                        if (shapes[k].Color == best && !shapes[k].IsInk && shapes[k].Contains(cx, cyy)) { sh = shapes[k]; break; }
                    sh = sh ?? firstShape[best];
                    outp[idx] = Shade(best, sh.ToneAt(cx, cyy));
                    filled[idx] = true;
                }
            // 바깥 윤곽선 (그림과 같은 방식). 아래쪽 가장자리는 몸이 잘린 곳이라 선을 두르지 않는다.
            var add = new List<int>();
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    if (filled[i]) continue;
                    if ((x > 0 && filled[i - 1]) || (x < n - 1 && filled[i + 1]) || (y > 0 && filled[i - n]) || (y < n - 1 && filled[i + n]))
                        add.Add(i);
                }
            foreach (int i in add) outp[i] = LineColor;
            return outp;
        }

        static uint Lerp(uint a, uint b, float t)
        {
            uint r = (uint)Math.Round(((a >> 16) & 255) + ((((b >> 16) & 255) - (float)((a >> 16) & 255)) * t));
            uint g = (uint)Math.Round(((a >> 8) & 255) + ((((b >> 8) & 255) - (float)((a >> 8) & 255)) * t));
            uint bl = (uint)Math.Round((a & 255) + (((b & 255) - (float)(a & 255)) * t));
            return 0xFF000000u | (r << 16) | (g << 8) | bl;
        }

        /// <summary>그림과 같은 명암 공식: 밝음 = 흰색 쪽 32%, 어두움 = 선 색 쪽 30%</summary>
        static uint Shade(uint c, int tone)
        {
            if (tone == 0) return Lerp(c, 0xFFFFFFFF, 0.32f);
            if (tone == 2) return Lerp(c, LineColor, 0.3f);
            return c;
        }
    }
}
