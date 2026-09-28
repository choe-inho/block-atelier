using System.Collections.Generic;

namespace BlockAtelier.Core
{
    /// <summary>8x8 보드. 칸 값은 Cell 참고. 인덱스는 y*Size+x.</summary>
    public sealed class BoardModel
    {
        public const int Size = 8;
        readonly int[] cells;

        public BoardModel() { cells = new int[Size * Size]; }
        BoardModel(int[] c) { cells = c; }

        public BoardModel Clone() { return new BoardModel((int[])cells.Clone()); }

        public int this[int x, int y]
        {
            get { return cells[y * Size + x]; }
            set { cells[y * Size + x] = value; }
        }

        public int Get(int index) { return cells[index]; }
        public void Set(int index, int value) { cells[index] = value; }

        public static int Index(int x, int y) { return y * Size + x; }

        public bool CanPlace(Shape shape, int ox, int oy)
        {
            if (ox < 0 || oy < 0 || ox + shape.Width > Size || oy + shape.Height > Size) return false;
            for (int i = 0; i < shape.Size; i++)
                if (cells[(oy + shape.Ys[i]) * Size + ox + shape.Xs[i]] != Cell.Empty) return false;
            return true;
        }

        public bool CanPlaceAnywhere(Shape shape)
        {
            for (int y = 0; y <= Size - shape.Height; y++)
                for (int x = 0; x <= Size - shape.Width; x++)
                    if (CanPlace(shape, x, y)) return true;
            return false;
        }

        /// <summary>블록을 놓고 놓인 칸 인덱스를 돌려준다. 먼저 CanPlace로 확인할 것.</summary>
        public List<int> Place(Shape shape, int color, int ox, int oy)
        {
            var placed = new List<int>(shape.Size);
            for (int i = 0; i < shape.Size; i++)
            {
                int idx = (oy + shape.Ys[i]) * Size + ox + shape.Xs[i];
                cells[idx] = color;
                placed.Add(idx);
            }
            return placed;
        }

        public bool IsRowFull(int y)
        {
            for (int x = 0; x < Size; x++) if (cells[y * Size + x] == Cell.Empty) return false;
            return true;
        }

        public bool IsColFull(int x)
        {
            for (int y = 0; y < Size; y++) if (cells[y * Size + x] == Cell.Empty) return false;
            return true;
        }

        /// <summary>한 줄이 모두 같은 색인지 (붓질). 회색이 섞이면 아니다. 만능 칸은 어느 색과도 맞는다.</summary>
        public bool IsMonochrome(IList<int> lineCells)
        {
            int color = 0;
            foreach (int idx in lineCells)
            {
                int v = cells[idx];
                if (v == Cell.Wild) continue;
                if (!Cell.IsColor(v)) return false;
                if (color == 0) color = v;
                else if (v != color) return false;
            }
            return color != 0;
        }

        public static List<int> RowCells(int y)
        {
            var l = new List<int>(Size);
            for (int x = 0; x < Size; x++) l.Add(y * Size + x);
            return l;
        }

        public static List<int> ColCells(int x)
        {
            var l = new List<int>(Size);
            for (int y = 0; y < Size; y++) l.Add(y * Size + x);
            return l;
        }

        public int Count(int value)
        {
            int n = 0;
            foreach (int v in cells) if (v == value) n++;
            return n;
        }

        public int FilledCount
        {
            get { int n = 0; foreach (int v in cells) if (v != Cell.Empty) n++; return n; }
        }
    }
}
