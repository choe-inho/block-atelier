using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BlockAtelier.Core
{
    /// <summary>
    /// 픽셀 사람 캐릭터(상반신) 조합. 부위마다 번호 하나씩, 문자열로 저장한다 ("s1h3c0e2m0o1p4a0b2").
    /// 그림과 같은 방식(벡터 도형 → 24x24, 자동 명암·윤곽선)으로 그려서 그림 앨범과 한 세트처럼 보인다.
    /// 부위 번호: 0 피부, 1 머리 모양, 2 머리 색, 3 눈, 4 입, 5 옷, 6 옷 색, 7 장식, 8 배경.
    /// 새 종류는 목록 끝에만 붙인다 (저장된 번호가 바뀌지 않게).
    /// </summary>
    public struct AvatarSpec
    {
        public int Skin, Hair, HairColor, Eyes, Mouth, Outfit, OutfitColor, Accessory, Background;

        public static readonly string[] SkinColors = { "#FFE3CC", "#F6CBA5", "#E3A77B", "#C08159", "#8D5A3B" };
        public static readonly string[] HairColors =
            { "#3A3346", "#6B4630", "#A0673F", "#E2B35F", "#DCDDE6", "#E05A47", "#F29BB1", "#5DA9E9", "#9B7FD4", "#7FD6C2", "#F28C38", "#81B29A" };
        public static readonly string[] OutfitColors =
            { "#5DA9E9", "#E05A47", "#81B29A", "#F6C85F", "#474556", "#F4F1EA", "#9B7FD4", "#F7A8B8", "#7FD6C2", "#F28C38", "#A0673F", "#3B3F58" };
        public static readonly string[] BackgroundColors = { "#EFE9DC", "#D6E6F5", "#F6D9DE", "#D5EFE3", "#FBEBC0", "#E4DAF3", "#F8DCC6", "#33385A" };
        public static readonly string[] HairNames =
            { "짧은 머리", "단발", "긴 머리", "올림머리", "삐죽 머리", "양갈래", "곱슬머리", "옆가르마", "포니테일", "땋은 머리", "까까머리", "모히칸" };
        public static readonly string[] EyeNames = { "동그란 눈", "웃는 눈", "반짝 눈", "졸린 눈", "윙크", "하트 눈", "진지한 눈" };
        public static readonly string[] MouthNames = { "미소", "활짝", "앙", "메롱", "고양이 입", "콧수염" };
        public static readonly string[] OutfitNames = { "티셔츠", "셔츠", "후드티", "줄무늬 티", "멜빵", "정장", "터틀넥" };
        public static readonly string[] AccessoryNames =
            { "없음", "선글라스", "리본", "헤드폰", "비니", "꽃핀", "고양이 귀", "토끼 귀", "왕관", "야구모자", "밀짚모자", "헤어밴드", "새싹" };

        public const string Keys = "shcemopab";
        public static readonly string[] PartNames = { "피부", "머리 모양", "머리 색", "눈", "입", "옷", "옷 색", "장식", "배경" };

        /// <summary>색으로 고르는 부위면 그 색 목록, 아니면 null</summary>
        public static string[] ColorsOf(int part)
        {
            return part == 0 ? SkinColors : part == 2 ? HairColors : part == 6 ? OutfitColors : part == 8 ? BackgroundColors : null;
        }

        /// <summary>모양으로 고르는 부위면 이름 목록, 아니면 null</summary>
        public static string[] NamesOf(int part)
        {
            return part == 1 ? HairNames : part == 3 ? EyeNames : part == 4 ? MouthNames : part == 5 ? OutfitNames : part == 7 ? AccessoryNames : null;
        }

        public int PartCount(int part)
        {
            var c = ColorsOf(part);
            return c != null ? c.Length : NamesOf(part).Length;
        }

        public int Get(int part)
        {
            switch (part)
            {
                case 0: return Skin; case 1: return Hair; case 2: return HairColor; case 3: return Eyes;
                case 4: return Mouth; case 5: return Outfit; case 6: return OutfitColor; case 7: return Accessory; default: return Background;
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
                case 6: s.OutfitColor = value; break; case 7: s.Accessory = value; break; default: s.Background = value; break;
            }
            return s;
        }

        /// <summary>처음 시작할 때 쓰는 무작위 캐릭터. 별 없이도 쓸 수 있는 것 중에서만 고른다.</summary>
        public static AvatarSpec Random(int seed)
        {
            var rng = new System.Random(seed);
            var s = new AvatarSpec();
            for (int p = 0; p < Keys.Length; p++)
            {
                var free = new List<int>();
                for (int v = 0; v < s.PartCount(p); v++) if (AvatarUnlocks.StarsFor(p, v) == 0) free.Add(v);
                s = s.With(p, free[rng.Next(free.Count)]);
            }
            return s;
        }

        /// <summary>지금 별로 열린 것 중에서 무작위 (꾸미기 화면의 '무작위' 버튼)</summary>
        public static AvatarSpec RandomUnlocked(int seed, int stars)
        {
            var rng = new System.Random(seed);
            var s = new AvatarSpec();
            for (int p = 0; p < Keys.Length; p++)
            {
                var open = new List<int>();
                for (int v = 0; v < s.PartCount(p); v++) if (AvatarUnlocks.IsUnlocked(p, v, stars)) open.Add(v);
                s = s.With(p, open[rng.Next(open.Count)]);
            }
            return s;
        }

        /// <summary>이 부위에서 dir 방향으로 다음 열린 값 (잠긴 것은 건너뜀). 열린 게 자기뿐이면 그대로.</summary>
        public AvatarSpec StepUnlocked(int part, int dir, int stars)
        {
            int n = PartCount(part), v = Get(part);
            for (int k = 1; k <= n; k++)
            {
                int cand = (((v + dir * k) % n) + n) % n;
                if (AvatarUnlocks.IsUnlocked(part, cand, stars)) return With(part, cand);
            }
            return this;
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

        /// <summary>잘못된 문자열은 null. 범위를 벗어난 번호는 범위 안으로 접는다. 나중에 생긴 부위(배경)는 빠져 있어도 0.</summary>
        public static AvatarSpec? Parse(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            var s = new AvatarSpec();
            int i = 0;
            var seen = new bool[Keys.Length];
            while (i < code.Length)
            {
                int part = Keys.IndexOf(code[i]);
                if (part < 0) return null;
                int j = i + 1;
                while (j < code.Length && char.IsDigit(code[j])) j++;
                int v;
                if (j == i + 1 || !int.TryParse(code.Substring(i + 1, j - i - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return null;
                s = s.With(part, v);
                seen[part] = true;
                i = j;
            }
            for (int p = 0; p < 8; p++) if (!seen[p]) return null;
            return s;
        }

        /// <summary>잠긴 부위는 별 없이 쓸 수 있는 첫 값으로 되돌린다 (진행 초기화 뒤 등)</summary>
        public AvatarSpec ClampToUnlocked(int stars)
        {
            var s = this;
            for (int p = 0; p < Keys.Length; p++)
                if (!AvatarUnlocks.IsUnlocked(p, s.Get(p), stars)) s = s.With(p, 0);
            return s;
        }
    }

    /// <summary>
    /// 별을 모을수록 꾸미기가 하나씩 열린다. 피부색은 처음부터 전부 열려 있다.
    /// 첫 보상은 별 3개(1~2판), 이후 약 5개마다 하나, 마지막 왕관은 별 270개.
    /// </summary>
    public static class AvatarUnlocks
    {
        public struct Entry { public int Part, Value, Stars; }

        // 처음부터 열린 개수 (부위 순서대로). 피부는 전부.
        static readonly int[] FreeCount = { 99, 4, 5, 2, 2, 2, 6, 1, 3 };

        // 열리는 순서: 눈에 띄는 장식을 앞쪽에, 부위를 골고루 섞는다. (부위, 번호)
        static readonly int[,] Order =
        {
            {7,6},{2,5},{3,4},{8,3},{1,4},{6,6},{7,2},{4,3},{1,5},{7,3},
            {2,6},{5,2},{7,12},{3,2},{8,4},{1,6},{7,7},{4,4},{6,7},{2,7},
            {7,1},{5,3},{1,8},{7,5},{3,3},{8,5},{2,8},{7,4},{4,2},{6,8},
            {1,7},{5,4},{7,9},{3,5},{2,9},{8,6},{1,9},{7,11},{6,9},{4,5},
            {2,10},{5,5},{7,10},{1,10},{3,6},{6,10},{8,7},{2,11},{5,6},{1,11},
            {6,11},{7,8},
        };

        public const int FirstStars = 3, LastStars = 270;
        static Entry[] schedule;

        public static Entry[] Schedule
        {
            get
            {
                if (schedule != null) return schedule;
                int n = Order.GetLength(0);
                schedule = new Entry[n];
                for (int k = 0; k < n; k++)
                    schedule[k] = new Entry
                    {
                        Part = Order[k, 0], Value = Order[k, 1],
                        Stars = (int)Math.Round(FirstStars + (LastStars - FirstStars) * (double)k / (n - 1)),
                    };
                return schedule;
            }
        }

        public static bool IsFree(int part, int value) { return value < FreeCount[part]; }

        /// <summary>그 꾸미기를 여는 데 필요한 별. 처음부터 열려 있으면 0.</summary>
        public static int StarsFor(int part, int value)
        {
            if (IsFree(part, value)) return 0;
            foreach (var e in Schedule) if (e.Part == part && e.Value == value) return e.Stars;
            return 0;
        }

        public static bool IsUnlocked(int part, int value, int stars) { return stars >= StarsFor(part, value); }

        /// <summary>별이 prev에서 now로 늘면서 새로 열린 것들</summary>
        public static List<Entry> Between(int prevStars, int nowStars)
        {
            var list = new List<Entry>();
            foreach (var e in Schedule) if (e.Stars > prevStars && e.Stars <= nowStars) list.Add(e);
            return list;
        }

        /// <summary>다음에 열릴 것. 다 열렸으면 null.</summary>
        public static Entry? Next(int stars)
        {
            foreach (var e in Schedule) if (e.Stars > stars) return e;
            return null;
        }

        public static int UnlockedCount(int stars)
        {
            int n = 0;
            foreach (var e in Schedule) if (e.Stars <= stars) n++;
            return n;
        }

        public static string NameOf(Entry e)
        {
            var names = AvatarSpec.NamesOf(e.Part);
            if (names != null) return names[e.Value];
            return "새 " + AvatarSpec.PartNames[e.Part];
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
            if (s.Hair == 8) c.Cap(74, 30, 86, 64, 14, hair).Mode = ShadeMode.Sphere;   // 포니테일 (머리 뒤)
            if (s.Hair == 6)                                                     // 곱슬머리 뒤쪽 부피
                for (int a = 160; a <= 380; a += 22)
                {
                    double r = a * Math.PI / 180.0;
                    c.Ellipse(50 + 29 * (float)Math.Cos(r), 44 + 27 * (float)Math.Sin(r), 10, 10, hair);
                }

            // 몸, 목
            if (s.Outfit == 2) c.Ellipse(50, 78, 22, 9, cloth).Tone = 2;          // 후드 모자 (목 뒤)
            c.Rect(10, 76, 90, 118, 22, s.Outfit == 4 ? white : cloth);           // 멜빵은 흰 티 위에
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
            if (s.Outfit == 3)   // 줄무늬 티
            {
                c.Ellipse(50, 77, 6, 3, skin, ShadeMode.Flat).Tone = 2;
                c.Rect(10, 83, 90, 87.5f, 0, white, ShadeMode.Flat);
                c.Rect(10, 92, 90, 96.5f, 0, white, ShadeMode.Flat);
            }
            if (s.Outfit == 4)   // 멜빵: 가슴판 + 멜빵끈 + 단추
            {
                c.Rect(32, 88, 68, 118, 4, cloth);
                c.Cap(36, 90, 30, 77, 5, cloth);
                c.Cap(64, 90, 70, 77, 5, cloth);
                c.Ellipse(38, 92, 2.2f, 2.2f, Hex("#F6C85F"), ShadeMode.Flat);
                c.Ellipse(62, 92, 2.2f, 2.2f, Hex("#F6C85F"), ShadeMode.Flat);
            }
            if (s.Outfit == 5)   // 정장: 흰 셔츠 V + 빨간 넥타이
            {
                c.Poly(new float[] { 38, 76, 62, 76, 50, 100 }, white, ShadeMode.Flat);
                c.Poly(new float[] { 47.5f, 79, 52.5f, 79, 54, 96, 50, 102, 46, 96 }, Hex("#E05A47"), ShadeMode.Flat);
            }
            if (s.Outfit == 6) c.Rect(40, 68, 60, 81, 5, cloth).Tone = 2;         // 터틀넥 목

            // 귀, 머리
            // 얼굴은 명암 없이 평평하게 (명암을 넣으면 뺨에 얼룩처럼 보인다)
            c.Ellipse(26, 50, 6, 7, skin, ShadeMode.Flat);
            c.Ellipse(74, 50, 6, 7, skin, ShadeMode.Flat);
            c.Ellipse(hx, hy, hrx, hry, skin, ShadeMode.Flat);

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
                case 3: c.InkCap(exl - 4, ey + 1, exl + 4, ey + 1, 2.6f); c.InkCap(exr - 4, ey + 1, exr + 4, ey + 1, 2.6f); break;
                case 4: c.InkDot(exl, ey, 3.3f); c.InkArc(exr, ey + 3, 4.5f, 2.6f, 200, 340); break;          // 윙크
                case 5:                                                                                        // 하트 눈
                {
                    uint hr = Hex("#E0406A");
                    foreach (float ex in new[] { exl, exr })
                    {
                        c.Ellipse(ex - 2.6f, ey - 1.6f, 3.1f, 3.1f, hr, ShadeMode.Flat).IsInk = true;
                        c.Ellipse(ex + 2.6f, ey - 1.6f, 3.1f, 3.1f, hr, ShadeMode.Flat).IsInk = true;
                        var tri = c.Poly(new float[] { ex - 5.4f, ey - 0.8f, ex + 5.4f, ey - 0.8f, ex, ey + 5.6f }, hr, ShadeMode.Flat);
                        tri.IsInk = true;
                    }
                    break;
                }
                default:                                                                                       // 진지한 눈: 눈 + 눈썹
                    c.InkDot(exl, ey + 1, 3.1f); c.InkDot(exr, ey + 1, 3.1f);
                    c.InkCap(exl - 4.5f, ey - 5.5f, exl + 3.5f, ey - 4, 2.6f); c.InkCap(exr - 3.5f, ey - 4, exr + 4.5f, ey - 5.5f, 2.6f);
                    break;
            }

            // 입
            switch (s.Mouth)
            {
                case 0: c.InkArc(50, 57, 4.5f, 2.6f, 20, 160); break;
                case 1: c.Ellipse(50, 61, 4.5f, 3.2f, mouthRed, ShadeMode.Flat).IsInk = true; break;
                case 2: c.InkCap(47, 61, 53, 61, 2.6f); break;
                case 3:                                                                   // 메롱
                    c.InkArc(50, 57, 4.5f, 2.6f, 20, 160);
                    c.Ellipse(52, 63, 2.6f, 3f, Hex("#F07C8F"), ShadeMode.Flat).IsInk = true;
                    break;
                case 4: c.InkArc(47.4f, 59, 2.8f, 2.2f, 0, 180); c.InkArc(52.6f, 59, 2.8f, 2.2f, 0, 180); break;   // 고양이 입
                default:                                                                  // 콧수염
                {
                    var l = c.Ellipse(45, 59, 5, 2.6f, hair, ShadeMode.Flat); l.IsInk = true;
                    var r = c.Ellipse(55, 59, 5, 2.6f, hair, ShadeMode.Flat); r.IsInk = true;
                    break;
                }
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
                case 5:   // 양갈래: 앞머리 + 옆 묶음(뒤에 그림)
                    c.Poly(CapPoly(hx, hy, hrx + 2, hry + 3, new float[] { 76, 48, 70, 38, 60, 40, 50, 36, 40, 40, 30, 38, 24, 48 }), hair, ShadeMode.Sphere);
                    break;
                case 6:   // 곱슬머리: 동글동글한 덩어리들
                    for (int a = 195; a <= 345; a += 25)
                    {
                        double r = a * Math.PI / 180.0;
                        c.Ellipse(50 + 23 * (float)Math.Cos(r), 42 + 19 * (float)Math.Sin(r), 9, 8, hair);
                    }
                    c.Ellipse(50, 22, 12, 9, hair);
                    break;
                case 7:   // 옆가르마: 한쪽으로 길게 넘긴 앞머리
                    c.Poly(CapPoly(hx, hy, hrx + 2, hry + 3, new float[] { 76, 50, 72, 36, 62, 34, 36, 46, 30, 40, 24, 50 }), hair, ShadeMode.Sphere);
                    break;
                case 8:   // 포니테일: 이마를 드러내고 뒤로 묶음 + 머리끈
                    c.Poly(CapPoly(hx, hy, hrx + 2, hry + 2, new float[] { 76, 46, 70, 33, 50, 28, 30, 33, 24, 46 }), hair, ShadeMode.Sphere);
                    c.Ellipse(75, 30, 3, 3, Hex("#F6C85F"), ShadeMode.Flat);
                    break;
                case 9:   // 땋은 머리: 앞머리 + 양쪽으로 땋아 내린 머리
                    c.Poly(CapPoly(hx, hy, hrx + 2, hry + 3, new float[] { 76, 50, 72, 38, 58, 37, 50, 40, 40, 36, 28, 38, 24, 50 }), hair, ShadeMode.Sphere);
                    for (int k = 0; k < 4; k++)
                    {
                        c.Ellipse(25, 60 + k * 8, 5.2f, 4.6f, hair);
                        c.Ellipse(75, 60 + k * 8, 5.2f, 4.6f, hair);
                    }
                    c.Ellipse(25, 93, 2.5f, 2.5f, Hex("#F7A8B8"), ShadeMode.Flat);
                    c.Ellipse(75, 93, 2.5f, 2.5f, Hex("#F7A8B8"), ShadeMode.Flat);
                    break;
                case 10:  // 까까머리: 머리 모양 그대로 얇게
                    c.Poly(CapPoly(hx, hy, hrx + 1, hry + 1, new float[] { 76, 44, 70, 34, 30, 34, 24, 44 }), hair, ShadeMode.Sphere);
                    break;
                default:  // 모히칸: 까까머리 + 가운데 높이 솟은 머리
                    c.Poly(CapPoly(hx, hy, hrx + 1, hry + 1, new float[] { 76, 44, 70, 34, 30, 34, 24, 44 }), hair, ShadeMode.Sphere).Tone = 2;
                    c.Poly(new float[] { 42, 36, 40, 16, 46, 6, 50, 2, 54, 6, 60, 16, 58, 36 }, hair, ShadeMode.Sphere);
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
                case 6:   // 고양이 귀 (머리 색)
                    c.Poly(new float[] { 26, 36, 28, 10, 46, 26 }, hair);
                    c.Poly(new float[] { 74, 36, 72, 10, 54, 26 }, hair);
                    c.Poly(new float[] { 31, 30, 32, 18, 40, 26 }, Hex("#F29BB1"), ShadeMode.Flat);
                    c.Poly(new float[] { 69, 30, 68, 18, 60, 26 }, Hex("#F29BB1"), ShadeMode.Flat);
                    break;
                case 7:   // 토끼 귀
                    c.Ellipse(38, 15, 6, 11, white);
                    c.Ellipse(62, 15, 6, 11, white);
                    c.Ellipse(38, 16, 2.6f, 7, Hex("#F29BB1"), ShadeMode.Flat);
                    c.Ellipse(62, 16, 2.6f, 7, Hex("#F29BB1"), ShadeMode.Flat);
                    break;
                case 8:   // 왕관
                    c.Poly(new float[] { 30, 30, 30, 12, 40, 22, 50, 8, 60, 22, 70, 12, 70, 30 }, Hex("#F6C85F"));
                    c.Ellipse(50, 22, 3, 3, Hex("#E05A47"), ShadeMode.Flat);
                    c.Ellipse(38, 25, 2.2f, 2.2f, Hex("#5DA9E9"), ShadeMode.Flat);
                    c.Ellipse(62, 25, 2.2f, 2.2f, Hex("#5DA9E9"), ShadeMode.Flat);
                    break;
                case 9:   // 야구모자 (옷 색)
                {
                    uint capc = s.OutfitColor == 5 ? Hex("#5DA9E9") : cloth;
                    c.Poly(CapPoly(hx, 40, hrx + 3, hry + 1, new float[] { 78, 40, 22, 40 }), capc, ShadeMode.Sphere);
                    c.Rect(44, 35, 92, 42, 3.5f, capc).Tone = 2;
                    c.Ellipse(50, 17, 2.5f, 2.5f, white, ShadeMode.Flat);
                    break;
                }
                case 10:  // 밀짚모자
                {
                    uint straw = Hex("#E9C46A");
                    c.Ellipse(50, 34, 44, 9, straw);
                    c.Poly(CapPoly(50, 34, 24, 20, new float[] { 74, 34, 26, 34 }), straw, ShadeMode.Sphere);
                    c.Rect(26, 26, 74, 32, 2, Hex("#E05A47"), ShadeMode.Flat);
                    break;
                }
                case 11:  // 헤어밴드
                    c.Arc(50, 46, 26, 6, 196, 344, Hex("#F7A8B8"));
                    break;
                case 12:  // 새싹
                {
                    uint leaf = Hex("#81B29A");
                    c.Cap(50, 24, 50, 14, 3.5f, leaf);
                    c.Ellipse(42, 12, 8, 4.5f, leaf);
                    c.Ellipse(58, 12, 8, 4.5f, leaf);
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
