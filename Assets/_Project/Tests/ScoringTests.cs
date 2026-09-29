using System.Collections.Generic;
using BlockAtelier.Core;
using NUnit.Framework;

namespace BlockAtelier.Tests
{
    /// <summary>별·시간 판정과 진행 기록(병합, 잠금, 저장) 검증.</summary>
    [TestFixture]
    public class ScoringTests
    {
        static LevelData Level(int id, int three, int two)
        {
            var lv = new LevelData { Id = id, PictureName = "t", PictureWidth = 2, PictureHeight = 1, Star3Moves = three, Star2Moves = two };
            lv.Palette.AddRange(new[] { "transparent", "#FF0000" });
            lv.Rows.Add("11");
            lv.Sequence.Add(new Block("O1", 1));
            lv.Validate();
            return lv;
        }

        [Test]
        public void 별은_수_기준으로_매기고_이어하기는_별_하나()
        {
            var lv = Level(1, 20, 26);
            Assert.AreEqual(3, Scoring.Stars(lv, 18, 0));
            Assert.AreEqual(3, Scoring.Stars(lv, 20, 0));
            Assert.AreEqual(2, Scoring.Stars(lv, 21, 0));
            Assert.AreEqual(2, Scoring.Stars(lv, 26, 0));
            Assert.AreEqual(1, Scoring.Stars(lv, 27, 0));
            Assert.AreEqual(1, Scoring.Stars(lv, 10, 1));
            Assert.AreEqual(3, Scoring.Stars(Level(1, 0, 0), 99, 0));   // 기준 없는 레벨
        }

        [Test]
        public void 다음_별까지_남은_수()
        {
            var lv = Level(1, 20, 26);
            int s;
            Assert.AreEqual(5, Scoring.MovesLeftForStars(lv, 15, 0, out s)); Assert.AreEqual(3, s);
            Assert.AreEqual(4, Scoring.MovesLeftForStars(lv, 22, 0, out s)); Assert.AreEqual(2, s);
            Assert.AreEqual(-1, Scoring.MovesLeftForStars(lv, 30, 0, out s)); Assert.AreEqual(1, s);
        }

        [Test]
        public void 시계는_시작_전과_멈춤_중에는_세지_않고_큰_프레임은_자른다()
        {
            var c = new PlayClock();
            c.Tick(1.0, true);
            Assert.AreEqual(0, c.ElapsedMs);
            c.Start();
            for (int i = 0; i < 60; i++) c.Tick(1 / 60.0, true);
            c.Tick(0.3, false);        // 연출 중
            c.Tick(30.0, true);        // 앱 복귀 순간의 큰 프레임
            Assert.AreEqual(1500, c.ElapsedMs, 2);
        }

        [Test]
        public void 기록은_좋은_쪽만_남고_새_기록을_알려준다()
        {
            var p = new Progress();
            var lv = Level(1, 20, 26);
            var r1 = p.Submit(lv, 24, 0, 50000);
            Assert.IsTrue(r1.FirstClear); Assert.IsTrue(r1.NewBestTime); Assert.AreEqual(2, r1.Stars);
            var r2 = p.Submit(lv, 30, 0, 40000);                      // 별은 적지만 더 빠름
            Assert.IsTrue(r2.NewBestTime); Assert.IsFalse(r2.MoreStars);
            Assert.AreEqual(2, p.Get(1).Stars); Assert.AreEqual(40000, p.Get(1).BestTimeMs);
            var r3 = p.Submit(lv, 10, 1, 1000);                       // 이어하기: 시간 제외
            Assert.IsFalse(r3.TimeCounted); Assert.IsFalse(r3.NewBestTime);
            Assert.AreEqual(40000, p.Get(1).BestTimeMs);
            p.Submit(lv, 18, 0, 45000);
            Assert.AreEqual(3, p.Get(1).Stars); Assert.AreEqual(40000, p.Get(1).BestTimeMs);
        }

        [Test]
        public void 잠금은_앞_레벨을_깨면_풀린다()
        {
            var p = new Progress();
            Assert.IsTrue(p.IsUnlocked(1)); Assert.IsFalse(p.IsUnlocked(2));
            p.Submit(Level(1, 20, 26), 30, 1, 9000);
            Assert.IsTrue(p.IsUnlocked(2)); Assert.IsFalse(p.IsUnlocked(3));
            Assert.AreEqual(2, p.FrontierLevel(100));
        }

        [Test]
        public void 병합은_레벨마다_좋은_쪽이고_순서와_무관하다()
        {
            var a = new Progress(); var b = new Progress();
            a.Set(1, new LevelRecord { Stars = 3, BestTimeMs = 60000 });
            a.Set(2, new LevelRecord { Stars = 1, BestTimeMs = -1 });
            b.Set(1, new LevelRecord { Stars = 2, BestTimeMs = 30000 });
            b.Set(3, new LevelRecord { Stars = 2, BestTimeMs = 20000 });
            var ab = Progress.FromJson(a.ToJson()); ab.Merge(b);
            var ba = Progress.FromJson(b.ToJson()); ba.Merge(a);
            Assert.AreEqual(ab.ToJson(), ba.ToJson());
            Assert.AreEqual(3, ab.Get(1).Stars); Assert.AreEqual(30000, ab.Get(1).BestTimeMs);
            Assert.AreEqual(-1, ab.Get(2).BestTimeMs);
            Assert.AreEqual(6, ab.TotalStars);
            ab.Merge(b);                                                // 같은 기록을 또 합쳐도 그대로
            Assert.AreEqual(ba.ToJson(), ab.ToJson());
        }

        [Test]
        public void 저장_형식은_되읽어도_같다()
        {
            var p = new Progress();
            p.Set(7, new LevelRecord { Stars = 3, BestTimeMs = 42370 });
            p.Set(12, new LevelRecord { Stars = 1, BestTimeMs = -1 });
            var json = p.ToJson();
            StringAssert.Contains("\"7\":[3,42370]", json);
            var q = Progress.FromJson(json);
            Assert.AreEqual(json, q.ToJson());
            Assert.AreEqual(0, Progress.FromJson("").TotalStars);
            Assert.AreEqual(0, Progress.FromJson("{깨진").TotalStars);
        }

        [Test]
        public void 시간_표시()
        {
            Assert.AreEqual("0:42", Scoring.FormatTime(42370));
            Assert.AreEqual("1:05", Scoring.FormatTime(65000));
            Assert.AreEqual("42.37초", Scoring.FormatTimePrecise(42370));
            Assert.AreEqual("1:05.2", Scoring.FormatTimePrecise(65200));
        }

        [Test]
        public void 레벨_JSON은_별_기준을_읽고_쓴다()
        {
            var lv = Level(5, 18, 24);
            var back = LevelData.FromJson(lv.ToJson());
            Assert.AreEqual(18, back.Star3Moves);
            Assert.AreEqual(24, back.Star2Moves);
        }
    }
}
