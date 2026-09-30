using System.Collections.Generic;
using BlockAtelier.Core;
using NUnit.Framework;

namespace BlockAtelier.Tests
{
    /// <summary>닉네임 규칙, 프로필·설정 저장, 앨범 요약.</summary>
    [TestFixture]
    public class ProfileTests
    {
        [Test]
        public void 닉네임_규칙()
        {
            Assert.IsTrue(Nickname.Validate("인호") == null);
            Assert.IsTrue(Nickname.Validate("Inho99") == null);
            Assert.IsTrue(Nickname.Validate("  졸린판다  ") == null);       // 앞뒤 공백은 잘라서 본다
            Assert.IsFalse(Nickname.Validate("호") == null);
            Assert.IsFalse(Nickname.Validate("열한글자가넘는닉네임이야") == null);
            Assert.IsFalse(Nickname.Validate("판다 고래") == null);         // 가운데 공백
            Assert.IsFalse(Nickname.Validate("판다!") == null);
            Assert.IsFalse(Nickname.Validate("ㅋㅋ") == null);              // 자모만
            Assert.IsFalse(Nickname.Validate("나는운영자") == null);
            Assert.IsFalse(Nickname.Validate("ADMIN1") == null);            // 대소문자 무시
        }

        [Test]
        public void 추천_닉네임은_항상_규칙을_통과하고_같은_시드면_같다()
        {
            for (int s = 0; s < 500; s++) Assert.IsTrue(Nickname.Validate(Nickname.Suggest(s)) == null, Nickname.Suggest(s));
            Assert.AreEqual(Nickname.Suggest(42), Nickname.Suggest(42));
        }

        [Test]
        public void 프로필은_잘못된_이름을_거절하고_저장을_되읽는다()
        {
            var p = PlayerProfile.CreateGuest(7);
            string before = p.Nickname;
            Assert.IsFalse(p.TrySetNickname("!!") == null);
            Assert.AreEqual(before, p.Nickname);
            Assert.IsTrue(p.TrySetNickname(" 새이름 ") == null);
            Assert.AreEqual("새이름", p.Nickname);
            p.Look = p.Look.With(1, 4).With(7, 2);
            var q = PlayerProfile.FromJson(p.ToJson(), 1);
            Assert.AreEqual("새이름", q.Nickname);
            Assert.AreEqual(p.Look.Code, q.Look.Code);
            Assert.AreEqual(Nickname.Suggest(3), PlayerProfile.FromJson("{깨진", 3).Nickname);
        }

        [Test]
        public void 캐릭터_코드는_되읽어도_같고_범위를_넘으면_접힌다()
        {
            var a = AvatarSpec.Random(5);
            Assert.AreEqual(a.Code, AvatarSpec.Parse(a.Code).Value.Code);
            Assert.AreEqual(AvatarSpec.Random(5).Code, a.Code);
            Assert.IsTrue(AvatarSpec.Parse("s1h2") == null);          // 부위가 빠짐
            Assert.IsTrue(AvatarSpec.Parse("x1h2c0e0m0o0p0a0") == null);
            Assert.AreEqual(0, a.With(1, a.PartCount(1)).Hair);        // 한 바퀴 돌면 처음으로
            Assert.AreEqual(a.PartCount(0) - 1, a.With(0, -1).Skin);
        }

        [Test]
        public void 모든_캐릭터_부위를_그릴_수_있다()
        {
            var s = new AvatarSpec();
            for (int part = 0; part < AvatarSpec.Keys.Length; part++)
                for (int v = 0; v < s.PartCount(part); v++)
                {
                    var px = AvatarArt.Render(s.With(part, v));
                    Assert.AreEqual(AvatarArt.Size * AvatarArt.Size, px.Length);
                    int filled = 0;
                    foreach (var c in px) if (c != 0) filled++;
                    Assert.IsTrue(filled > 200, "part " + part + " value " + v);
                    Assert.AreEqual(0u, px[0]);                                 // 왼쪽 위 모서리는 비어 있음
                }
        }

        [Test]
        public void 잠긴_꾸미기는_모두_한_번씩_별로_열린다()
        {
            var spec = new AvatarSpec();
            var seen = new HashSet<string>();
            foreach (var e in AvatarUnlocks.Schedule)
            {
                Assert.IsFalse(AvatarUnlocks.IsFree(e.Part, e.Value), "무료인데 목록에 있음 " + e.Part + ":" + e.Value);
                Assert.IsTrue(e.Value < spec.PartCount(e.Part));
                Assert.IsTrue(seen.Add(e.Part + ":" + e.Value), "중복 " + e.Part + ":" + e.Value);
            }
            for (int p = 0; p < AvatarSpec.Keys.Length; p++)
                for (int v = 0; v < spec.PartCount(p); v++)
                    Assert.IsTrue(AvatarUnlocks.IsFree(p, v) || seen.Contains(p + ":" + v), "열 방법이 없음 " + p + ":" + v);
            var sch = AvatarUnlocks.Schedule;
            Assert.AreEqual(AvatarUnlocks.FirstStars, sch[0].Stars);
            Assert.AreEqual(AvatarUnlocks.LastStars, sch[sch.Length - 1].Stars);
            for (int i = 1; i < sch.Length; i++) Assert.IsTrue(sch[i].Stars > sch[i - 1].Stars);
        }

        [Test]
        public void 별이_늘면_새로_열린_것을_알려준다()
        {
            Assert.AreEqual(0, AvatarUnlocks.Between(0, 2).Count);
            var first = AvatarUnlocks.Between(0, 3);
            Assert.AreEqual(1, first.Count);
            Assert.AreEqual("고양이 귀", AvatarUnlocks.NameOf(first[0]));
            Assert.IsFalse(AvatarUnlocks.IsUnlocked(first[0].Part, first[0].Value, 2));
            Assert.IsTrue(AvatarUnlocks.IsUnlocked(first[0].Part, first[0].Value, 3));
            Assert.AreEqual(AvatarUnlocks.Schedule.Length, AvatarUnlocks.Between(0, 300).Count);
            Assert.IsTrue(AvatarUnlocks.Next(300) == null);
            Assert.AreEqual(AvatarUnlocks.Schedule[1].Stars, AvatarUnlocks.Next(3).Value.Stars);
        }

        [Test]
        public void 무작위_캐릭터는_열린_것만_쓰고_잠긴_것은_되돌린다()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var a = AvatarSpec.Random(seed);
                for (int p = 0; p < AvatarSpec.Keys.Length; p++) Assert.IsTrue(AvatarUnlocks.IsUnlocked(p, a.Get(p), 0));
            }
            var crown = new AvatarSpec().With(7, 8).With(1, 11);
            var clamped = crown.ClampToUnlocked(10);
            Assert.AreEqual(0, clamped.Accessory);
            Assert.AreEqual(0, clamped.Hair);
            Assert.AreEqual(8, crown.ClampToUnlocked(300).Accessory);
            Assert.AreEqual(0, AvatarSpec.Parse("s1h1c1e2m0o2p0a0").Value.Background);   // 예전 저장(배경 없음)도 읽힘
        }

        [Test]
        public void 설정_저장()
        {
            var s = new GameSettings { Sound = false, ReduceMotion = true };
            var t = GameSettings.FromJson(s.ToJson());
            Assert.IsFalse(t.Sound);
            Assert.IsTrue(t.ReduceMotion);
            Assert.IsTrue(GameSettings.FromJson("").Sound);
        }

        static LevelData L(int id, string album)
        {
            var lv = new LevelData { Id = id, Album = album, AlbumTitle = album + "앨범", PictureName = "t", PictureWidth = 1, PictureHeight = 1 };
            lv.Palette.AddRange(new[] { "transparent", "#FF0000" });
            lv.Rows.Add("1");
            lv.Sequence.Add(new Block("O1", 1));
            return lv;
        }

        [Test]
        public void 앨범_요약은_진행과_잠금과_표지를_계산한다()
        {
            var levels = new List<LevelData> { L(1, "a"), L(2, "a"), L(3, "a"), L(4, "b"), L(5, "b") };
            var p = new Progress();
            p.Set(1, new LevelRecord { Stars = 3, BestTimeMs = 1000 });
            p.Set(2, new LevelRecord { Stars = 1, BestTimeMs = -1 });
            var s = AlbumSummary.Build(levels, p);
            Assert.AreEqual(2, s.Count);
            Assert.AreEqual(3, s[0].Count);
            Assert.AreEqual(2, s[0].Cleared);
            Assert.AreEqual(4, s[0].Stars);
            Assert.AreEqual(1, s[0].CoverIndex);        // 가장 최근에 완성한 그림
            Assert.IsTrue(s[0].Unlocked);
            Assert.IsFalse(s[1].Unlocked);              // 3레벨을 아직 못 깸
            p.Set(3, new LevelRecord { Stars = 2, BestTimeMs = -1 });
            s = AlbumSummary.Build(levels, p);
            Assert.IsTrue(s[0].Complete);
            Assert.IsTrue(s[1].Unlocked);
            Assert.AreEqual(3, s[1].FirstIndex);
            Assert.IsFalse(s[1].CoverCleared);
        }
    }
}
