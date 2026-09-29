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
