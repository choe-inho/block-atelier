using System.Collections.Generic;

namespace BlockAtelier.Core
{
    /// <summary>
    /// 칠해야 할 픽셀 그림. 페인트 한 방울 = 픽셀 하나.
    /// 같은 색 안에서는 아래 줄부터 위로, 왼쪽부터 오른쪽으로 칠한다 (물이 차오르는 느낌).
    /// </summary>
    public sealed class PictureModel
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int ColorCount;

        readonly int[] target;          // 픽셀별 목표 색 (0 = 칠할 필요 없음)
        readonly bool[] painted;
        readonly List<int>[] order;     // 색별 칠하는 순서
        readonly int[] nextInOrder;     // 색별로 다음에 칠할 order 위치
        readonly int[] total;

        public PictureModel(LevelData level)
        {
            Width = level.PictureWidth;
            Height = level.PictureHeight;
            ColorCount = level.ColorCount;
            target = new int[Width * Height];
            painted = new bool[Width * Height];
            order = new List<int>[ColorCount + 1];
            nextInOrder = new int[ColorCount + 1];
            total = new int[ColorCount + 1];
            for (int c = 0; c <= ColorCount; c++) order[c] = new List<int>();

            for (int y = Height - 1; y >= 0; y--)
                for (int x = 0; x < Width; x++)
                {
                    int c = level.PixelAt(x, y);
                    int idx = y * Width + x;
                    target[idx] = c;
                    if (c > 0)
                    {
                        order[c].Add(idx);
                        total[c]++;
                    }
                }
        }

        PictureModel(PictureModel o)
        {
            Width = o.Width;
            Height = o.Height;
            ColorCount = o.ColorCount;
            target = o.target;       // 불변이라 공유
            order = o.order;         // 불변이라 공유
            total = o.total;
            painted = (bool[])o.painted.Clone();
            nextInOrder = (int[])o.nextInOrder.Clone();
        }

        public PictureModel Clone() { return new PictureModel(this); }

        public int TargetAt(int x, int y) { return target[y * Width + x]; }
        public bool IsPainted(int x, int y) { return painted[y * Width + x]; }
        public bool IsPaintedIndex(int idx) { return painted[idx]; }

        public int Total(int color) { return total[color]; }
        public int Remaining(int color) { return total[color] - nextInOrder[color]; }

        public int TotalPixels
        {
            get { int s = 0; for (int c = 1; c <= ColorCount; c++) s += total[c]; return s; }
        }

        public int RemainingPixels
        {
            get { int s = 0; for (int c = 1; c <= ColorCount; c++) s += Remaining(c); return s; }
        }

        public float Progress { get { int t = TotalPixels; return t == 0 ? 1f : 1f - (float)RemainingPixels / t; } }

        public bool IsComplete { get { return RemainingPixels == 0; } }

        public bool NeedsColor(int color)
        {
            return color >= 1 && color <= ColorCount && Remaining(color) > 0;
        }

        /// <summary>남은 픽셀이 가장 많은 색. 만능 칸과 이어하기 블록 색 고를 때 쓴다. 없으면 0.</summary>
        public int MostNeededColor()
        {
            int best = 0, bestRemain = 0;
            for (int c = 1; c <= ColorCount; c++)
            {
                int r = Remaining(c);
                if (r > bestRemain) { best = c; bestRemain = r; }
            }
            return best;
        }

        /// <summary>
        /// 페인트 한 방울을 떨어뜨린다. 칠해진 픽셀 인덱스(y*Width+x)를, 필요 없는 색이면 -1을 돌려준다.
        /// </summary>
        public int ApplyPaint(int color)
        {
            if (color == Cell.Wild) color = MostNeededColor();
            if (!NeedsColor(color)) return -1;
            int idx = order[color][nextInOrder[color]++];
            painted[idx] = true;
            return idx;
        }
    }
}
