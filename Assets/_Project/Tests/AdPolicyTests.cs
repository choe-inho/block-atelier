using BlockAtelier.Core;
using NUnit.Framework;

namespace BlockAtelier.Tests
{
    /// <summary>전면 광고 판단, 보상형 제공 규칙, 실험군, 저장.</summary>
    [TestFixture]
    public class AdPolicyTests
    {
        const long Min = 60 * 1000L;

        // 광고를 보여도 되는 상황 하나를 만들어 두고 조건을 하나씩 깨 본다.
        static AdState Veteran()
        {
            return new AdState { InstallPlayMs = 30 * Min, LastAdAtMs = 0, LevelsSinceInterstitial = 5, Day = "2026-10-01", DayCount = 1 };
        }

        static AdContext Ctx(long now = 100 * Min)
        {
            return new AdContext { LevelNumber = 20, FailsInLevel = 0, LevelsThisSession = 3, InterstitialsThisSession = 0, NowMs = now, Today = "2026-10-01" };
        }

        [Test]
        public void 조건을_모두_채우면_보여_준다()
        {
            Assert.IsNull(AdPolicy.Decide(new AdConfig(), Veteran(), Ctx()));
        }

        [Test]
        public void 새_사용자와_세션_첫_판과_어렵게_깬_판은_건너뛴다()
        {
            var c = new AdConfig();
            var x = Ctx(); x.LevelNumber = 12;
            Assert.AreEqual("new_user_level", AdPolicy.Decide(c, Veteran(), x));
            var s = Veteran(); s.InstallPlayMs = 7 * Min;
            Assert.AreEqual("new_user_time", AdPolicy.Decide(c, s, Ctx()));
            x = Ctx(); x.LevelsThisSession = 1;
            Assert.AreEqual("first_of_session", AdPolicy.Decide(c, Veteran(), x));
            x = Ctx(); x.FailsInLevel = 2;
            Assert.AreEqual("hard_win", AdPolicy.Decide(c, Veteran(), x));
            x = Ctx(); x.FailsInLevel = 1;
            Assert.IsNull(AdPolicy.Decide(c, Veteran(), x));
        }

        [Test]
        public void 간격과_횟수_제한()
        {
            var c = new AdConfig();
            var s = Veteran(); s.LevelsSinceInterstitial = 1;
            Assert.AreEqual("level_gap", AdPolicy.Decide(c, s, Ctx()));

            s = Veteran(); s.LastAdAtMs = 100 * Min - 149 * 1000L;
            Assert.AreEqual("time_gap", AdPolicy.Decide(c, s, Ctx()));
            s.LastAdAtMs = 100 * Min - 150 * 1000L;
            Assert.IsNull(AdPolicy.Decide(c, s, Ctx()));

            var x = Ctx(); x.InterstitialsThisSession = 4;
            Assert.AreEqual("session_cap", AdPolicy.Decide(c, Veteran(), x));

            s = Veteran(); s.DayCount = 10;
            Assert.AreEqual("daily_cap", AdPolicy.Decide(c, s, Ctx()));
            x = Ctx(); x.Today = "2026-10-02";                         // 날이 바뀌면 다시 0
            Assert.IsNull(AdPolicy.Decide(c, s, x));
        }

        [Test]
        public void 보상형을_본_뒤에도_시간_간격을_지킨다()
        {
            var c = new AdConfig();
            var s = Veteran();
            AdPolicy.OnRewardedShown(s, 100 * Min - 30 * 1000L);
            Assert.AreEqual("time_gap", AdPolicy.Decide(c, s, Ctx()));
        }

        [Test]
        public void 광고_직후_나가는_일이_쌓이면_간격이_두_배()
        {
            var c = new AdConfig();
            var s = Veteran();
            AdPolicy.OnInterstitialShown(s, 10 * Min, "2026-10-01");
            Assert.IsTrue(AdPolicy.OnAppLeft(c, s, 10 * Min + 5000));
            Assert.IsFalse(AdPolicy.OnAppLeft(c, s, 10 * Min + 11000));  // 10초가 지나면 광고 탓 아님
            Assert.AreEqual(150 * 1000L, AdPolicy.IntervalMs(c, s));
            Assert.IsTrue(AdPolicy.OnAppLeft(c, s, 10 * Min + 1000));
            Assert.AreEqual(300 * 1000L, AdPolicy.IntervalMs(c, s));
        }

        [Test]
        public void 광고를_보면_기록이_바뀌고_두_판_뒤에야_다시_나온다()
        {
            var c = new AdConfig();
            var s = Veteran();
            AdPolicy.OnInterstitialShown(s, 100 * Min, "2026-10-01");
            Assert.AreEqual(2, s.DayCount);
            Assert.AreEqual(0, s.LevelsSinceInterstitial);
            var x = Ctx(200 * Min);
            AdPolicy.OnLevelComplete(s);
            Assert.AreEqual("level_gap", AdPolicy.Decide(c, s, x));
            AdPolicy.OnLevelComplete(s);
            Assert.IsNull(AdPolicy.Decide(c, s, x));
        }

        [Test]
        public void 광고_제거를_사면_전면_광고가_없고_힌트를_받는다()
        {
            var c = new AdConfig();
            var s = Veteran();
            s.HintsUsed = 3;
            Assert.AreEqual(Offer.Ad, AdPolicy.HintOffer(c, s));
            AdPolicy.OnNoAdsPurchased(c, s);
            AdPolicy.OnNoAdsPurchased(c, s);                         // 두 번 불려도 한 번만
            Assert.AreEqual("no_ads", AdPolicy.Decide(c, s, Ctx()));
            Assert.AreEqual(5, AdPolicy.FreeHintsLeft(c, s));
            Assert.AreEqual(Offer.Free, AdPolicy.HintOffer(c, s));
        }

        [Test]
        public void 이어하기는_10레벨까지_무료_그_뒤_판당_두_번()
        {
            var c = new AdConfig();
            Assert.AreEqual(Offer.Free, AdPolicy.ContinueOffer(c, 10, 5));
            Assert.AreEqual(Offer.Ad, AdPolicy.ContinueOffer(c, 11, 0));
            Assert.AreEqual(Offer.Ad, AdPolicy.ContinueOffer(c, 11, 1));
            Assert.AreEqual(Offer.None, AdPolicy.ContinueOffer(c, 11, 2));
        }

        [Test]
        public void 실험군과_원격_설정()
        {
            Assert.AreEqual("disabled", AdPolicy.Decide(AdConfig.ForGroup(AdConfig.Control), Veteran(), Ctx()));
            var a = AdConfig.ForGroup(AdConfig.Aggressive);
            var x = Ctx(); x.LevelNumber = 9;
            Assert.IsNull(AdPolicy.Decide(a, Veteran(), x));
            Assert.AreEqual(5, a.SessionCap);

            var o = new AdConfig().WithOverrides("{\"MinLevel\": 30, \"SkipFirstLevelOfSession\": false, \"Unknown\": 1, \"SessionCap\": \"x\"}");
            Assert.AreEqual(30, o.MinLevel);
            Assert.IsFalse(o.SkipFirstLevelOfSession);
            Assert.AreEqual(4, o.SessionCap);
            Assert.AreEqual(13, new AdConfig().WithOverrides("{깨진").MinLevel);

            // 같은 설치 id는 늘 같은 군, 비율은 대략 지켜진다
            var w = new[] { 1, 1, 1 };
            Assert.AreEqual(Experiment.Assign("abc", "ads1", w, AdConfig.Groups), Experiment.Assign("abc", "ads1", w, AdConfig.Groups));
            int control = 0;
            for (int i = 0; i < 3000; i++) if (Experiment.Assign("id" + i, "ads1", w, AdConfig.Groups) == AdConfig.Control) control++;
            Assert.IsTrue(control > 850 && control < 1150, "control " + control);
            Assert.AreEqual(AdConfig.Base, Experiment.Assign("abc", "ads1", new[] { 0, 1, 0 }, AdConfig.Groups));
        }

        [Test]
        public void 광고_기록_저장()
        {
            var s = Veteran();
            s.QuitAfterAdCount = 2; s.HintsUsed = 4; s.HintBonus = 5; s.NoAds = true; s.InstallPlayMs = 123456789L;
            var t = AdState.FromJson(s.ToJson());
            Assert.AreEqual(s.ToJson(), t.ToJson());
            Assert.AreEqual(-1, AdState.FromJson("").LastAdAtMs);
            Assert.AreEqual(999, AdState.FromJson("{깨진").LevelsSinceInterstitial);
        }
    }
}
