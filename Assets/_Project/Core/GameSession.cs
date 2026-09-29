using System.Collections.Generic;

namespace BlockAtelier.Core
{
    public enum GameState { Playing, Won, Lost }

    public struct ClearedCell
    {
        public int Index;
        public int Color;          // 지워지기 전 칸 값
        public int Multiplier;     // 붓질(단색 줄)이면 2
        public bool FromExplosion; // 십자 폭발로 지워진 칸
    }

    public struct PaintEvent
    {
        public int FromCell;   // 보드 칸 인덱스
        public int Color;      // 칸 색 (만능이면 Cell.Wild)
        public int Pixel;      // 칠해진 첫 픽셀 인덱스, 필요 없는 색이면 -1
        public List<int> Pixels; // 이 방울이 칠한 픽셀 전부 (붓 크기만큼)
    }

    /// <summary>한 수의 결과. 뷰는 이 순서대로 연출만 하면 된다.</summary>
    public sealed class MoveResult
    {
        public Block Block;
        public List<int> Placed = new List<int>();
        public List<int> ClearedRows = new List<int>();
        public List<int> ClearedCols = new List<int>();
        public List<int> MonochromeRows = new List<int>();
        public List<int> MonochromeCols = new List<int>();
        public List<ClearedCell> Cleared = new List<ClearedCell>();
        public List<PaintEvent> Paints = new List<PaintEvent>();
        public List<int> Grayed = new List<int>();
        public List<int> CompletedColors = new List<int>();
        public bool Exploded;
        /// <summary>다 칠한 색의 블록이 컨베이어·보관함에서 다른 색으로 바뀌었는지</summary>
        public bool ConveyorRecolored;
        public GameState StateAfter;

        public int LinesCleared { get { return ClearedRows.Count + ClearedCols.Count; } }

        public int PixelsPainted
        {
            get { int n = 0; foreach (var p in Paints) if (p.Pixel >= 0) n += p.Pixels.Count; return n; }
        }

        /// <summary>그림에 닿은 방울 수 (버려진 방울 제외)</summary>
        public int DropsLanded
        {
            get { int n = 0; foreach (var p in Paints) if (p.Pixel >= 0) n++; return n; }
        }
    }

    /// <summary>
    /// 한 판의 규칙 전체. UnityEngine에 의존하지 않아 자동 풀이 봇과 테스트가 그대로 쓴다.
    /// 규칙은 기획서 3장(코어 게임플레이 규칙)과 같다.
    /// </summary>
    public sealed class GameSession
    {
        public readonly LevelData Level;
        public BoardModel Board { get; private set; }
        public PictureModel Picture { get; private set; }
        public ConveyorModel Conveyor { get; private set; }
        public GameState State { get; private set; }
        public int MovesUsed { get; private set; }
        public int ContinuesUsed { get; private set; }

        bool[] completed;
        readonly Stack<Snapshot> history = new Stack<Snapshot>();

        struct Snapshot
        {
            public BoardModel Board;
            public PictureModel Picture;
            public ConveyorModel Conveyor;
            public bool[] Completed;
            public GameState State;
            public int Moves;
        }

        public GameSession(LevelData level)
        {
            Level = level;
            Board = new BoardModel();
            Picture = new PictureModel(level);
            Conveyor = new ConveyorModel(level.Sequence);
            Conveyor.ColorFilter = FilterColor;
            completed = new bool[level.ColorCount + 1];
            State = GameState.Playing;
            // 팔레트에 있지만 그림에 없는 색은 처음부터 완료 처리.
            for (int c = 1; c <= level.ColorCount; c++)
                if (Picture.Total(c) == 0) MarkCompleted(c, null);
        }

        GameSession(GameSession o)
        {
            Level = o.Level;
            Board = o.Board.Clone();
            Picture = o.Picture.Clone();
            Conveyor = o.Conveyor.Clone();
            Conveyor.ColorFilter = FilterColor;
            completed = (bool[])o.completed.Clone();
            State = o.State;
            MovesUsed = o.MovesUsed;
            ContinuesUsed = o.ContinuesUsed;
        }

        /// <summary>되돌리기 기록 없이 현재 상태만 복사. 봇 시뮬레이션용.</summary>
        public GameSession Clone() { return new GameSession(this); }

        public bool IsColorCompleted(int color) { return completed[color]; }

        // ---------------- 조회 ----------------

        public Block GetBlock(BlockSource src) { return Conveyor.Get(src); }

        public bool CanPlace(BlockSource src, int x, int y)
        {
            if (State != GameState.Playing) return false;
            Block b = Conveyor.Get(src);
            return !b.IsNone && Board.CanPlace(b.Shape, x, y);
        }

        public bool CanPlaceAnywhere(BlockSource src)
        {
            Block b = Conveyor.Get(src);
            return !b.IsNone && Board.CanPlaceAnywhere(b.Shape);
        }

        public bool HasAnyMove()
        {
            foreach (var src in Conveyor.AllSources())
                if (CanPlaceAnywhere(src)) return true;
            return Conveyor.CanRevealByStash;
        }

        public bool CanUndo { get { return history.Count > 0; } }

        // ---------------- 행동 ----------------

        /// <summary>블록을 (x, y)에 놓는다. 놓을 수 없으면 null.</summary>
        public MoveResult TryPlace(BlockSource src, int x, int y)
        {
            if (!CanPlace(src, x, y)) return null;
            PushHistory();

            var r = new MoveResult();
            Block block = Conveyor.Take(src);
            r.Block = block;
            r.Placed = Board.Place(block.Shape, block.Color, x, y);

            ResolveLines(r);
            MovesUsed++;
            UpdateState();
            r.StateAfter = State;
            return r;
        }

        /// <summary>컨베이어 앞쪽 블록을 보관함에 넣거나 맞바꾼다.</summary>
        public bool TryStash(int conveyorIndex)
        {
            if (State != GameState.Playing) return false;
            if (conveyorIndex < 0 || conveyorIndex >= Conveyor.SelectableSlots) return false;
            PushHistory();
            Conveyor.Stash(conveyorIndex);
            UpdateState();
            return true;
        }

        /// <summary>되돌리기 부스터. 직전 행동(놓기 또는 보관) 이전 상태로.</summary>
        public bool Undo()
        {
            if (history.Count == 0) return false;
            var s = history.Pop();
            Board = s.Board;
            Picture = s.Picture;
            Conveyor = s.Conveyor;
            completed = s.Completed;
            State = s.State;
            MovesUsed = s.Moves;
            return true;
        }

        /// <summary>무지개 세탁 부스터. 회색 칸을 최대 maxCells개 만능 칸으로 바꾼다. 바뀐 칸 인덱스를 돌려준다.</summary>
        public List<int> UseRainbowWash(int maxCells = 3)
        {
            var changed = new List<int>();
            if (State != GameState.Playing) return changed;
            for (int i = 0; i < BoardModel.Size * BoardModel.Size && changed.Count < maxCells; i++)
            {
                if (Board.Get(i) != Cell.Gray) continue;
                Board.Set(i, Cell.Wild);
                changed.Add(i);
            }
            if (changed.Count > 0) history.Clear();
            return changed;
        }

        /// <summary>보관함 +1 부스터. 이번 판 보관 칸 2개.</summary>
        public void UseExtraHold()
        {
            Conveyor.SetHoldCapacity(2);
            if (State == GameState.Lost) UpdateStateAllowRevive();
        }

        /// <summary>
        /// 이어하기 (광고 또는 코인). 실패 상태에서만. 회색 칸 3개를 지우고,
        /// 컨베이어 앞 3칸을 작은 블록으로 바꾼다. 작은 블록은 가장 필요한 색.
        /// </summary>
        public bool Continue()
        {
            if (State != GameState.Lost) return false;

            int removed = 0;
            for (int i = 0; i < BoardModel.Size * BoardModel.Size && removed < 3; i++)
                if (Board.Get(i) == Cell.Gray) { Board.Set(i, Cell.Empty); removed++; }

            int c1 = Picture.MostNeededColor();
            int c2 = SecondNeededColor(c1);
            var rescue = new List<Block>
            {
                new Block("O1", c1),
                new Block("I2H", c1),
                new Block("I2V", c2),
            };
            Conveyor.ReplaceFront(rescue);

            ContinuesUsed++;
            history.Clear();
            State = GameState.Playing;
            UpdateState();
            return true;
        }

        // ---------------- 내부 ----------------

        void ResolveLines(MoveResult r)
        {
            for (int y = 0; y < BoardModel.Size; y++)
                if (Board.IsRowFull(y)) r.ClearedRows.Add(y);
            for (int x = 0; x < BoardModel.Size; x++)
                if (Board.IsColFull(x)) r.ClearedCols.Add(x);

            if (r.LinesCleared == 0) return;

            // 칸별 배수 (붓질이면 2). 지우기 전에 판정한다.
            var mult = new Dictionary<int, int>();
            var ordered = new List<int>();
            foreach (int y in r.ClearedRows)
            {
                var line = BoardModel.RowCells(y);
                bool mono = Board.IsMonochrome(line);
                if (mono) r.MonochromeRows.Add(y);
                AddLine(line, mono ? 2 : 1, mult, ordered);
            }
            foreach (int x in r.ClearedCols)
            {
                var line = BoardModel.ColCells(x);
                bool mono = Board.IsMonochrome(line);
                if (mono) r.MonochromeCols.Add(x);
                AddLine(line, mono ? 2 : 1, mult, ordered);
            }

            foreach (int idx in ordered)
                r.Cleared.Add(new ClearedCell { Index = idx, Color = Board.Get(idx), Multiplier = mult[idx] });

            // 십자 폭발: 가로와 세로가 함께 지워지면 교차점 주변 3x3.
            if (r.ClearedRows.Count > 0 && r.ClearedCols.Count > 0)
            {
                r.Exploded = true;
                foreach (int y in r.ClearedRows)
                    foreach (int x in r.ClearedCols)
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= BoardModel.Size || ny >= BoardModel.Size) continue;
                                int idx = BoardModel.Index(nx, ny);
                                if (mult.ContainsKey(idx) || Board.Get(idx) == Cell.Empty) continue;
                                mult[idx] = 1;
                                r.Cleared.Add(new ClearedCell { Index = idx, Color = Board.Get(idx), Multiplier = 1, FromExplosion = true });
                            }
            }

            // 페인트
            foreach (var c in r.Cleared)
            {
                if (c.Color == Cell.Gray) continue;
                for (int k = 0; k < c.Multiplier; k++)
                {
                    var px = new List<int>(Picture.Brush);
                    Picture.ApplyPaint(c.Color, px);
                    r.Paints.Add(new PaintEvent { FromCell = c.Index, Color = c.Color, Pixel = px.Count > 0 ? px[0] : -1, Pixels = px });
                }
            }

            foreach (var c in r.Cleared) Board.Set(c.Index, Cell.Empty);

            // 다 칠한 색 처리
            for (int col = 1; col <= Level.ColorCount; col++)
            {
                if (completed[col] || Picture.Remaining(col) > 0) continue;
                r.CompletedColors.Add(col);
                if (MarkCompleted(col, r.Grayed)) r.ConveyorRecolored = true;
            }
        }

        static void AddLine(List<int> line, int m, Dictionary<int, int> mult, List<int> ordered)
        {
            foreach (int idx in line)
            {
                int cur;
                if (mult.TryGetValue(idx, out cur)) { if (m > cur) mult[idx] = m; }
                else { mult[idx] = m; ordered.Add(idx); }
            }
        }

        /// <summary>
        /// 다 칠한 색의 블록 처리. 회색 규칙이 켜진 레벨은 회색으로,
        /// 초반 레벨(꺼짐)은 아직 필요한 색 중 가장 많이 남은 색으로 바꿔 준다.
        /// </summary>
        int FilterColor(int c)
        {
            if (c < 1 || c > Level.ColorCount || !completed[c]) return c;
            if (Level.GrayOnComplete) return Cell.Gray;
            int m = Picture.MostNeededColor();
            return m == 0 ? c : m;
        }

        bool MarkCompleted(int color, List<int> grayed)
        {
            completed[color] = true;
            if (!Level.GrayOnComplete)
            {
                int m = Picture.MostNeededColor();
                if (m == 0) return false;
                bool had = HasOnConveyor(color);
                Conveyor.Recolor(color, m);
                return had;
            }
            for (int i = 0; i < BoardModel.Size * BoardModel.Size; i++)
            {
                if (Board.Get(i) != color) continue;
                Board.Set(i, Cell.Gray);
                if (grayed != null) grayed.Add(i);
            }
            bool any = HasOnConveyor(color);
            Conveyor.Recolor(color, Cell.Gray);
            return any;
        }

        bool HasOnConveyor(int color)
        {
            for (int i = 0; i < Conveyor.VisibleSlots; i++) if (Conveyor.Visible(i).Color == color) return true;
            return Conveyor.Hold(0).Color == color && !Conveyor.Hold(0).IsNone || Conveyor.Hold(1).Color == color && !Conveyor.Hold(1).IsNone;
        }

        void UpdateState()
        {
            if (State != GameState.Playing) return;
            if (Picture.IsComplete) { State = GameState.Won; return; }
            if (Conveyor.IsEmpty || !HasAnyMove()) State = GameState.Lost;
        }

        void UpdateStateAllowRevive()
        {
            State = GameState.Playing;
            UpdateState();
        }

        int SecondNeededColor(int first)
        {
            int best = first, bestRemain = 0;
            for (int c = 1; c <= Level.ColorCount; c++)
            {
                if (c == first) continue;
                int r = Picture.Remaining(c);
                if (r > bestRemain) { best = c; bestRemain = r; }
            }
            return best == 0 ? 1 : best;
        }

        void PushHistory()
        {
            history.Push(new Snapshot
            {
                Board = Board.Clone(),
                Picture = Picture.Clone(),
                Conveyor = Conveyor.Clone(),
                Completed = (bool[])completed.Clone(),
                State = State,
                Moves = MovesUsed,
            });
        }
    }
}
