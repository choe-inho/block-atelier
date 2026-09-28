using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlockAtelier.Core
{
    /// <summary>컨베이어에 흘러오는 블록 하나. Color는 팔레트 인덱스 또는 Cell.Gray.</summary>
    public struct Block
    {
        public readonly string ShapeCode;
        public readonly int Color;

        public Block(string shapeCode, int color)
        {
            ShapeCode = shapeCode;
            Color = color;
        }

        public Shape Shape { get { return Shape.Get(ShapeCode); } }
        public bool IsNone { get { return ShapeCode == null; } }
        public static readonly Block None = new Block(null, 0);

        public Block WithColor(int color) { return new Block(ShapeCode, color); }
        public override string ToString() { return IsNone ? "(없음)" : ShapeCode + ":" + Color; }
    }

    /// <summary>레벨 JSON 한 개. 형식은 block-atelier-levels 스킬 참고.</summary>
    public sealed class LevelData
    {
        public int Id;
        public string Album = "";
        public string Difficulty = "normal";
        public double TargetClearRate = 0.9;
        public List<string> Mechanics = new List<string>();

        /// <summary>한 색을 다 칠하면 보드에 남은 그 색 블록을 회색으로 바꿀지. 초반 레벨은 false.</summary>
        public bool GrayOnComplete = true;

        public string PictureName = "";
        public int PictureWidth;
        public int PictureHeight;

        /// <summary>팔레트. 0번은 투명(칠할 필요 없음), 1번부터 그림 색. "#RRGGBB".</summary>
        public List<string> Palette = new List<string>();

        /// <summary>그림 픽셀. 행 단위 문자열, 문자 하나가 팔레트 인덱스(0~9).</summary>
        public List<string> Rows = new List<string>();

        public List<Block> Sequence = new List<Block>();

        public int PixelAt(int x, int y) { return Rows[y][x] - '0'; }

        public int ColorCount { get { return Palette.Count - 1; } }

        public static LevelData FromJson(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object>;
            if (root == null) throw new FormatException("레벨 JSON 최상위는 객체여야 함");

            var lv = new LevelData();
            lv.Id = Int(root, "id", 0);
            lv.Album = Str(root, "album", "");
            lv.Difficulty = Str(root, "difficulty", "normal");
            lv.TargetClearRate = Num(root, "targetClearRate", 0.9);
            lv.GrayOnComplete = Bool(root, "grayOnComplete", true);

            object mech;
            if (root.TryGetValue("mechanics", out mech) && mech is List<object>)
                foreach (var m in (List<object>)mech) lv.Mechanics.Add((string)m);

            var pic = root["picture"] as Dictionary<string, object>;
            if (pic == null) throw new FormatException("picture 없음");
            lv.PictureName = Str(pic, "name", "");
            lv.PictureWidth = Int(pic, "w", 0);
            lv.PictureHeight = Int(pic, "h", 0);
            foreach (var c in (List<object>)pic["palette"]) lv.Palette.Add((string)c);
            foreach (var r in (List<object>)pic["rows"]) lv.Rows.Add((string)r);

            foreach (var o in (List<object>)root["sequence"])
            {
                var b = (Dictionary<string, object>)o;
                lv.Sequence.Add(new Block((string)b["shape"], Int(b, "color", 1)));
            }

            lv.Validate();
            return lv;
        }

        public void Validate()
        {
            if (PictureWidth <= 0 || PictureHeight <= 0) throw new FormatException("그림 크기가 잘못됨");
            if (Rows.Count != PictureHeight) throw new FormatException("rows 줄 수가 h와 다름");
            if (Palette.Count < 2 || Palette.Count > 10) throw new FormatException("팔레트는 2~10개");
            foreach (var r in Rows)
            {
                if (r.Length != PictureWidth) throw new FormatException("rows 길이가 w와 다름: " + r);
                foreach (char ch in r)
                {
                    int v = ch - '0';
                    if (v < 0 || v >= Palette.Count) throw new FormatException("팔레트에 없는 색 인덱스: " + ch);
                }
            }
            if (Sequence.Count == 0) throw new FormatException("sequence가 비어 있음");
            foreach (var b in Sequence)
            {
                if (!Shape.Exists(b.ShapeCode)) throw new FormatException("알 수 없는 모양: " + b.ShapeCode);
                if (b.Color < 1 || b.Color >= Palette.Count) throw new FormatException("블록 색이 팔레트 밖: " + b);
            }
        }

        public string ToJson()
        {
            var seq = new List<object>();
            foreach (var b in Sequence)
                seq.Add(new Dictionary<string, object> { { "shape", b.ShapeCode }, { "color", b.Color } });

            var root = new Dictionary<string, object>
            {
                { "id", Id },
                { "album", Album },
                { "difficulty", Difficulty },
                { "targetClearRate", TargetClearRate },
                { "grayOnComplete", GrayOnComplete },
                { "mechanics", Mechanics },
                { "picture", new Dictionary<string, object>
                    {
                        { "name", PictureName },
                        { "w", PictureWidth },
                        { "h", PictureHeight },
                        { "palette", Palette },
                        { "rows", Rows },
                    }
                },
                { "sequence", seq },
            };
            return MiniJson.Serialize(root);
        }

        static int Int(Dictionary<string, object> d, string k, int def)
        {
            object v;
            return d.TryGetValue(k, out v) && v != null ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : def;
        }

        static double Num(Dictionary<string, object> d, string k, double def)
        {
            object v;
            return d.TryGetValue(k, out v) && v != null ? Convert.ToDouble(v, CultureInfo.InvariantCulture) : def;
        }

        static string Str(Dictionary<string, object> d, string k, string def)
        {
            object v;
            return d.TryGetValue(k, out v) && v != null ? (string)v : def;
        }

        static bool Bool(Dictionary<string, object> d, string k, bool def)
        {
            object v;
            return d.TryGetValue(k, out v) && v is bool ? (bool)v : def;
        }
    }
}
