using System.Collections.Generic;
using BlockAtelier.Core;
using BlockAtelier.Core.Sim;
using NUnit.Framework;

namespace BlockAtelier.Tests
{
    /// <summary>기획서 3장 규칙을 하나씩 검증한다. Unity Test Runner의 EditMode에서 실행.</summary>
    [TestFixture]
    public class CoreRulesTests
    {
        // 그림: 색1 20픽셀, 색2 4픽셀 (5x5)
        static LevelData MakeLevel(List<Block> seq, bool grayOnComplete = true)
        {
            var lv = new LevelData
            {
                Id = 999,
                PictureName = "test",
                PictureWidth = 5,
                PictureHeight = 5,
                GrayOnComplete = grayOnComplete,
            };
            lv.Palette.AddRange(new[] { "transparent", "#FF8800", "#223355" });
            lv.Rows.AddRange(new[] { "11111", "11111", "11111", "11111", "22220" });
            lv.Sequence.AddRange(seq);
            lv.Validate();
            return lv;
        }

        static List<Block> Repeat(string shape, int color, int n)
        {
            var l = new List<Block>();
            for (int i = 0; i < n; i++) l.Add(new Block(shape, color));
            return l;
        }

        static BlockSource C(int i) { return BlockSource.Conveyor(i); }

        [Test]
        public void 모양_좌표와_크기가_맞다()
        {
            Assert.AreEqual(9, Shape.Get("O9").Size);
            Assert.AreEqual(3, Shape.Get("O9").Width);
            Assert.AreEqual(5, Shape.Get("I5V").Height);
            Assert.AreEqual(4, Shape.Get("T4A").Size);
            foreach (var s in Shape.All)
                for (int i = 0; i < s.Size; i++)
                    for (int j = i + 1; j < s.Size; j++)
                        Assert.IsFalse(s.Xs[i] == s.Xs[j] && s.Ys[i] == s.Ys[j], s.Code + " 좌표 중복");
        }

        [Test]
        public void 레벨_JSON_왕복()
        {
            var lv = MakeLevel(Repeat("I4H", 1, 3));
            var back = LevelData.FromJson(lv.ToJson());
            Assert.AreEqual(lv.Rows.Count, back.Rows.Count);
            Assert.AreEqual("22220", back.Rows[4]);
            Assert.AreEqual(3, back.Sequence.Count);
            Assert.AreEqual("I4H", back.Sequence[0].ShapeCode);
            Assert.AreEqual(true, back.GrayOnComplete);
        }

        [Test]
        public void 보드_밖이나_겹치면_놓을_수_없다()
        {
            var g = new GameSession(MakeLevel(Repeat("I4H", 1, 10)));
            Assert.IsFalse(g.CanPlace(C(0), 5, 0));   // 4칸짜리가 x=5부터면 밖
            Assert.IsTrue(g.CanPlace(C(0), 4, 0));
            Assert.IsNotNull(g.TryPlace(C(0), 0, 0));
            Assert.IsFalse(g.CanPlace(C(0), 2, 0));   // 겹침
        }

        [Test]
        public void 컨베이어는_쓴_칸만큼_당겨지고_앞_3개만_고를_수_있다()
        {
            var seq = new List<Block>();
            for (int i = 1; i <= 8; i++) seq.Add(new Block("O1", 1 + i % 2));
            var g = new GameSession(MakeLevel(seq));
            Assert.AreEqual(5, g.Conveyor.VisibleSlots);
            Assert.AreEqual(3, g.Conveyor.SelectableSlots);
            Block third = g.Conveyor.Visible(2);
            Block fourth = g.Conveyor.Visible(3);
            g.TryPlace(C(1), 0, 0);
            Assert.AreEqual(third.Color, g.Conveyor.Visible(1).Color);
            Assert.AreEqual(fourth.Color, g.Conveyor.Visible(2).Color);
            Assert.AreEqual(5, g.Conveyor.VisibleSlots);
            Assert.AreEqual(2, g.Conveyor.RemainingInSequence);
        }

        [Test]
        public void 보관함에_넣으면_새_블록이_보이고_가득_차면_맞바꾼다()
        {
            var seq = new List<Block> {
                new Block("I2H", 1), new Block("I3H", 1), new Block("I4H", 1),
                new Block("I5H", 1), new Block("O1", 1), new Block("O4", 1) };
            var g = new GameSession(MakeLevel(seq));
            Assert.IsTrue(g.TryStash(0));
            Assert.AreEqual("I2H", g.Conveyor.Hold(0).ShapeCode);
            Assert.AreEqual("O4", g.Conveyor.Visible(4).ShapeCode);
            Assert.IsTrue(g.TryStash(0));  // 가득 참 → 맞바꿈
            Assert.AreEqual("I3H", g.Conveyor.Hold(0).ShapeCode);
            Assert.AreEqual("I2H", g.Conveyor.Visible(0).ShapeCode);
            Assert.IsNotNull(g.TryPlace(BlockSource.Held(0), 0, 0));
            Assert.IsTrue(g.Conveyor.Hold(0).IsNone);
        }

        [Test]
        public void 한_줄을_지우면_칸마다_페인트_한_방울()
        {
            // 색1 I4H 두 개로 한 줄, 첫 줄 섞인 색 → 붓질 아님을 확인하려고 색2 블록을 섞는다.
            var seq = new List<Block> { new Block("I4H", 1), new Block("I4H", 2), new Block("O1", 1), new Block("O1", 1) };
            var g = new GameSession(MakeLevel(seq));
            g.TryPlace(C(0), 0, 0);
            var r = g.TryPlace(C(0), 4, 0);
            Assert.AreEqual(1, r.ClearedRows.Count);
            Assert.AreEqual(0, r.MonochromeRows.Count);
            Assert.AreEqual(8, r.Paints.Count);
            Assert.AreEqual(8, r.PixelsPainted);          // 색1 4개, 색2 4개 모두 필요
            Assert.AreEqual(16, g.Picture.Remaining(1));
            Assert.AreEqual(0, g.Picture.Remaining(2));
            Assert.AreEqual(0, g.Board.FilledCount);
        }

        [Test]
        public void 같은_색_한_줄은_붓질로_2배()
        {
            var g = new GameSession(MakeLevel(Repeat("I4H", 1, 6)));
            g.TryPlace(C(0), 0, 0);
            var r = g.TryPlace(C(0), 4, 0);
            Assert.AreEqual(1, r.MonochromeRows.Count);
            Assert.AreEqual(16, r.Paints.Count);
            Assert.AreEqual(16, r.PixelsPainted);
            Assert.AreEqual(4, g.Picture.Remaining(1));
        }

        [Test]
        public void 필요_없는_색은_페인트가_버려진다()
        {
            var seq = new List<Block> { new Block("I4H", 2), new Block("I4H", 2), new Block("I4H", 2), new Block("I4H", 2), new Block("O1", 1) };
            var g = new GameSession(MakeLevel(seq, grayOnComplete: false));
            g.TryPlace(C(0), 0, 0);
            var r = g.TryPlace(C(0), 4, 0);   // 붓질 8칸 x2 = 16방울, 색2는 4픽셀뿐
            Assert.AreEqual(16, r.Paints.Count);
            Assert.AreEqual(4, r.PixelsPainted);
            CollectionAssert.Contains(r.CompletedColors, 2);
        }

        [Test]
        public void 다_칠한_색은_회색이_된다()
        {
            var seq = new List<Block> { new Block("I4H", 2), new Block("O1", 2), new Block("I4H", 2), new Block("O1", 1), new Block("O1", 1) };
            var g = new GameSession(MakeLevel(seq));
            g.TryPlace(C(1), 0, 7);           // 색2 한 칸을 맨 아래에 남겨 둔다
            g.TryPlace(C(0), 0, 0);
            var r = g.TryPlace(C(0), 4, 0);   // 색2 완성
            CollectionAssert.Contains(r.CompletedColors, 2);
            Assert.AreEqual(Cell.Gray, g.Board[0, 7]);
            CollectionAssert.Contains(r.Grayed, BoardModel.Index(0, 7));
        }

        [Test]
        public void 회색_옵션이_꺼져_있으면_색이_남는다()
        {
            var seq = new List<Block> { new Block("I4H", 2), new Block("O1", 2), new Block("I4H", 2), new Block("O1", 1) };
            var g = new GameSession(MakeLevel(seq, grayOnComplete: false));
            g.TryPlace(C(1), 0, 7);
            g.TryPlace(C(0), 0, 0);
            g.TryPlace(C(0), 4, 0);
            Assert.AreEqual(2, g.Board[0, 7]);
        }

        [Test]
        public void 가로와_세로를_함께_지우면_십자_폭발()
        {
            // 가로 0줄과 세로 0열을 같은 수에 완성시키고, (1,1)에 남은 칸이 폭발로 지워지는지 확인.
            var seq = new List<Block>
            {
                new Block("I4H", 1), new Block("I3H", 1), new Block("I4V", 1),
                new Block("I3V", 1), new Block("O1", 1), new Block("O1", 1), new Block("O1", 1),
            };
            var g = new GameSession(MakeLevel(seq));
            g.TryPlace(C(0), 4, 0);           // 가로 0줄 x4~7
            g.TryPlace(C(0), 1, 0);           // 가로 0줄 x1~3
            g.TryPlace(C(0), 0, 4);           // 세로 0열 y4~7
            g.TryPlace(C(0), 0, 1);           // 세로 0열 y1~3
            g.TryPlace(C(0), 1, 1);           // 폭발 범위 안의 한 칸
            var r = g.TryPlace(C(0), 0, 0);   // 모서리를 채워 가로 + 세로 동시 완성
            Assert.IsTrue(r.Exploded);
            Assert.AreEqual(1, r.ClearedRows.Count);
            Assert.AreEqual(1, r.ClearedCols.Count);
            Assert.AreEqual(Cell.Empty, g.Board[1, 1]);
            Assert.AreEqual(16, r.Cleared.Count);
        }

        [Test]
        public void 그림을_다_채우면_승리()
        {
            // 색1 20 + 색2 4 = 24픽셀. 색1 붓질 한 줄(16) + 섞인 줄(색1 4, 색2 4) = 24.
            var seq = new List<Block> {
                new Block("I4H", 1), new Block("I4H", 1),
                new Block("I4H", 1), new Block("I4H", 2), new Block("O1", 1) };
            var g = new GameSession(MakeLevel(seq));
            g.TryPlace(C(0), 0, 0);
            g.TryPlace(C(0), 4, 0);
            g.TryPlace(C(0), 0, 1);
            var r = g.TryPlace(C(0), 4, 1);
            Assert.AreEqual(GameState.Won, r.StateAfter);
            Assert.IsTrue(g.Picture.IsComplete);
        }

        [Test]
        public void 놓을_곳이_없고_보관도_못하면_실패_그리고_이어하기()
        {
            // 가로 2줄과 5줄을 7칸씩 채우면 줄은 안 지워지고 3x3이 들어갈 공간이 사라진다.
            var seq = new List<Block> { new Block("I5H", 1), new Block("I2H", 2), new Block("I5H", 1), new Block("I2H", 2) };
            for (int i = 0; i < 20; i++) seq.Add(new Block("O9", 1));
            var g = new GameSession(MakeLevel(seq));
            Assert.IsNotNull(g.TryPlace(C(0), 0, 2));
            Assert.IsNotNull(g.TryPlace(C(0), 5, 2));
            Assert.IsNotNull(g.TryPlace(C(0), 0, 5));
            Assert.IsNotNull(g.TryPlace(C(0), 5, 5));
            // 이제 O9만 남음 → 놓을 곳 없음, 보관 1칸 비었으니 아직 살아 있음
            Assert.AreEqual(GameState.Playing, g.State);
            Assert.IsTrue(g.TryStash(0));
            Assert.AreEqual(GameState.Lost, g.State);

            Assert.IsTrue(g.Continue());
            Assert.AreEqual(GameState.Playing, g.State);
            Assert.AreEqual("O1", g.Conveyor.Visible(0).ShapeCode);
            Assert.AreEqual(1, g.ContinuesUsed);
        }

        [Test]
        public void 되돌리기는_직전_상태로()
        {
            var g = new GameSession(MakeLevel(Repeat("I4H", 1, 6)));
            g.TryPlace(C(0), 0, 0);
            g.TryPlace(C(0), 4, 0);
            Assert.AreEqual(4, g.Picture.Remaining(1));
            Assert.IsTrue(g.Undo());
            Assert.AreEqual(20, g.Picture.Remaining(1));
            Assert.AreEqual(4, g.Board.FilledCount);
            Assert.AreEqual(1, g.MovesUsed);
        }

        [Test]
        public void 무지개_세탁은_회색을_만능으로_바꾸고_만능은_필요한_색을_칠한다()
        {
            var seq = new List<Block> { new Block("I4H", 2), new Block("O1", 2), new Block("I4H", 2), new Block("I4H", 1), new Block("I3H", 1), new Block("O1", 1) };
            var g = new GameSession(MakeLevel(seq));
            g.TryPlace(C(1), 0, 7);
            g.TryPlace(C(0), 0, 0);
            g.TryPlace(C(0), 4, 0);           // 색2 완성 → (0,7) 회색
            var changed = g.UseRainbowWash();
            Assert.AreEqual(1, changed.Count);
            Assert.AreEqual(Cell.Wild, g.Board[0, 7]);
            g.TryPlace(C(0), 1, 7);
            var r = g.TryPlace(C(0), 5, 7);   // 만능 포함 한 줄 → 붓질(2배) 16방울 모두 색1
            Assert.AreEqual(1, r.MonochromeRows.Count);
            Assert.AreEqual(16, r.PixelsPainted);
        }

        [Test]
        public void 봇은_쉬운_레벨을_깬다()
        {
            var seq = new List<Block>();
            for (int i = 0; i < 40; i++) seq.Add(new Block(i % 2 == 0 ? "I4H" : "I4V", i % 4 == 3 ? 2 : 1));
            var lv = MakeLevel(seq, grayOnComplete: false);
            double moves;
            double rate = AutoSolver.MeasureClearRate(lv, 5, 0, 1, out moves);
            Assert.AreEqual(1.0, rate);
        }
    }
}
