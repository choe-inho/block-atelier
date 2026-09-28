using System;
using System.Collections.Generic;

namespace BlockAtelier.Core.Sim
{
    /// <summary>
    /// 자동 풀이 봇. 레벨 난이도 측정(클리어율)과 나중에 힌트 기능에 쓴다.
    /// 매 수마다 가능한 모든 행동을 한 수 앞까지 시뮬레이션해 점수를 매기고,
    /// Noise만큼 흔들어 사람 같은 실수를 흉내 낸다. Noise 0이면 항상 최선(탐욕).
    /// </summary>
    public sealed class AutoSolver
    {
        public double Noise = 0;
        readonly Random rng;

        public AutoSolver(int seed, double noise)
        {
            rng = new Random(seed);
            Noise = noise;
        }

        public struct Action
        {
            public bool IsStash;
            public BlockSource Source;
            public int StashIndex;
            public int X, Y;
            public double Score;
        }

        /// <summary>한 판을 끝까지 둔다. 이어하기 없이. 승리 여부를 돌려준다.</summary>
        public bool PlayToEnd(GameSession game, int maxActions = 400)
        {
            for (int n = 0; n < maxActions && game.State == GameState.Playing; n++)
            {
                Action a;
                if (!ChooseAction(game, out a)) break;
                if (a.IsStash) game.TryStash(a.StashIndex);
                else game.TryPlace(a.Source, a.X, a.Y);
            }
            return game.State == GameState.Won;
        }

        public bool ChooseAction(GameSession game, out Action best)
        {
            best = default(Action);
            bool found = false;
            double bestScore = double.NegativeInfinity;

            foreach (var src in game.Conveyor.AllSources())
            {
                Block b = game.GetBlock(src);
                Shape s = b.Shape;
                for (int y = 0; y <= BoardModel.Size - s.Height; y++)
                    for (int x = 0; x <= BoardModel.Size - s.Width; x++)
                    {
                        if (!game.Board.CanPlace(s, x, y)) continue;
                        var sim = game.Clone();
                        var res = sim.TryPlace(src, x, y);
                        double score = Evaluate(sim, res) + Jitter();
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = new Action { Source = src, X = x, Y = y, Score = score };
                            found = true;
                        }
                    }
            }

            // 보관함에 넣어 새 블록을 보는 행동 (놓을 곳이 마땅치 않을 때 의미가 있다)
            if (game.Conveyor.CanRevealByStash)
            {
                for (int i = 0; i < game.Conveyor.SelectableSlots; i++)
                {
                    var sim = game.Clone();
                    sim.TryStash(i);
                    double score = Evaluate(sim, null) - 25 + Jitter();
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new Action { IsStash = true, StashIndex = i, Score = score };
                        found = true;
                    }
                }
            }
            return found;
        }

        double Jitter()
        {
            if (Noise <= 0) return 0;
            // 정규분포 근사 (Box-Muller)
            double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
            return Noise * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }

        /// <summary>행동 뒤 상태의 점수. 높을수록 좋다.</summary>
        public static double Evaluate(GameSession g, MoveResult r)
        {
            if (g.State == GameState.Won) return 1e6;
            if (g.State == GameState.Lost) return -1e6;

            double s = 0;
            if (r != null)
            {
                s += 60 * r.PixelsPainted;
                s += 15 * r.LinesCleared;
                // 필요 없는 색을 지워 페인트를 버린 경우 약한 감점
                s -= 4 * (r.Paints.Count - r.PixelsPainted);
            }

            var board = g.Board;
            int holes = 0, gray = 0;
            for (int y = 0; y < BoardModel.Size; y++)
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    int v = board[x, y];
                    if (v == Cell.Gray) gray++;
                    if (v != Cell.Empty) continue;
                    int blocked = 0;
                    if (x == 0 || board[x - 1, y] != Cell.Empty) blocked++;
                    if (x == BoardModel.Size - 1 || board[x + 1, y] != Cell.Empty) blocked++;
                    if (y == 0 || board[x, y - 1] != Cell.Empty) blocked++;
                    if (y == BoardModel.Size - 1 || board[x, y + 1] != Cell.Empty) blocked++;
                    if (blocked == 4) holes++;
                }
            s -= 10 * holes;
            s -= 3 * gray;
            s -= 1.5 * board.FilledCount;

            // 줄이 한 가지 색으로 모이면 붓질 기대 가치
            for (int i = 0; i < BoardModel.Size; i++)
            {
                s += LinePotential(g, BoardModel.RowCells(i));
                s += LinePotential(g, BoardModel.ColCells(i));
            }

            // 다음 블록들을 놓을 수 있는지
            int placeable = 0, total = 0;
            foreach (var src in g.Conveyor.AllSources())
            {
                total++;
                if (g.CanPlaceAnywhere(src)) placeable++;
            }
            s += 12 * placeable;
            if (placeable == 0 && total > 0) s -= 200;
            return s;
        }

        static double LinePotential(GameSession g, List<int> line)
        {
            int color = 0, filled = 0;
            foreach (int idx in line)
            {
                int v = g.Board.Get(idx);
                if (v == Cell.Empty) continue;
                filled++;
                if (!Cell.IsColor(v)) return 0;
                if (color == 0) color = v;
                else if (color != v) return 0;
            }
            if (color == 0 || !g.Picture.NeedsColor(color)) return 0;
            return 0.6 * filled;
        }

        /// <summary>레벨을 runs번 풀어 클리어율(0~1)을 잰다.</summary>
        public static double MeasureClearRate(LevelData level, int runs, double noise, int seed, out double avgMoves)
        {
            int wins = 0;
            long moves = 0;
            for (int i = 0; i < runs; i++)
            {
                var game = new GameSession(level);
                var bot = new AutoSolver(seed + i * 7919, noise);
                if (bot.PlayToEnd(game)) wins++;
                moves += game.MovesUsed;
            }
            avgMoves = runs == 0 ? 0 : (double)moves / runs;
            return runs == 0 ? 0 : (double)wins / runs;
        }
    }
}
