using System;
using System.Collections.Generic;

namespace BlockAtelier.Core
{
    /// <summary>
    /// 광고 수치. 전부 원격 설정으로 덮어쓸 수 있다 (block-atelier-monetization 스킬 표가 기본값).
    /// 실험군: control(전면 없음), base(기본), aggressive(9레벨부터, 120초, 세션 5번).
    /// </summary>
    public sealed class AdConfig
    {
        public const string Control = "control", Base = "base", Aggressive = "aggressive";
        public static readonly string[] Groups = { Control, Base, Aggressive };

        public string Group = Base;

        // 전면 광고 ('다음 레벨'로 넘어갈 때만)
        public bool InterstitialEnabled = true;
        public int MinLevel = 13;                 // 이 레벨을 깬 뒤부터
        public int MinInstallPlaySeconds = 480;   // 설치 후 누적 플레이 8분
        public int MinSecondsSinceLastAd = 150;   // 보상형을 본 뒤에도 적용
        public int MinLevelsSinceLastAd = 2;
        public int SessionCap = 4;
        public int DailyCap = 10;
        public int SkipAfterFailsInLevel = 2;     // 이만큼 실패한 끝에 깬 판 뒤엔 건너뜀
        public bool SkipFirstLevelOfSession = true;
        public int QuitAfterAdSeconds = 10;       // 광고 뒤 이 시간 안에 나가면 '광고 때문에 나감'
        public int QuitBackoffAfter = 2;          // 그런 일이 이만큼 쌓이면 간격 2배

        // 보상형
        public int FreeContinueUntilLevel = 10;   // 이 레벨까지 이어하기 무료
        public int ContinuesPerGame = 2;          // 그 뒤 판당 최대
        public int FreeUndosPerGame = 3;
        public int UndoRefill = 3;
        public int FreeHints = 3;                 // 평생 무료 힌트
        public int NoAdsHintBonus = 5;            // 광고 제거 구매 보너스 힌트

        public static AdConfig ForGroup(string group)
        {
            var c = new AdConfig();
            switch (group)
            {
                case Control:
                    c.Group = Control;
                    c.InterstitialEnabled = false;
                    break;
                case Aggressive:
                    c.Group = Aggressive;
                    c.MinLevel = 9;
                    c.MinSecondsSinceLastAd = 120;
                    c.SessionCap = 5;
                    break;
                default:
                    c.Group = Base;
                    break;
            }
            return c;
        }

        /// <summary>원격 설정 JSON의 값만 덮어쓴다. 모르는 키·잘못된 형식은 무시.</summary>
        public AdConfig WithOverrides(string json)
        {
            var c = (AdConfig)MemberwiseClone();
            Dictionary<string, object> root = null;
            try { root = string.IsNullOrEmpty(json) ? null : MiniJson.Parse(json) as Dictionary<string, object>; }
            catch (Exception) { }
            if (root == null) return c;
            foreach (var kv in root)
            {
                var f = typeof(AdConfig).GetField(kv.Key);
                if (f == null || f.IsLiteral || f.IsInitOnly) continue;
                try
                {
                    if (f.FieldType == typeof(int) && kv.Value is double) f.SetValue(c, (int)Math.Round((double)kv.Value));
                    else if (f.FieldType == typeof(int) && kv.Value is long) f.SetValue(c, (int)(long)kv.Value);
                    else if (f.FieldType == typeof(bool) && kv.Value is bool) f.SetValue(c, kv.Value);
                    else if (f.FieldType == typeof(string) && kv.Value is string) f.SetValue(c, kv.Value);
                }
                catch (Exception) { }
            }
            return c;
        }
    }

    /// <summary>폰에 저장하는 광고 기록.</summary>
    public sealed class AdState
    {
        public long InstallPlayMs;          // 설치 후 누적 플레이(앱이 앞에 있던 시간)
        public long LastAdAtMs = -1;        // 마지막 광고(전면·보상형)가 끝난 시각, 유닉스 ms
        public int LevelsSinceInterstitial = 999;
        public string Day = "";             // DayCount가 가리키는 날 (yyyy-MM-dd, 기기 현지)
        public int DayCount;                // 그날 전면 광고 수
        public int QuitAfterAdCount;        // 광고 직후 앱을 나간 횟수
        public int InterstitialsTotal;
        public int HintsUsed;
        public int HintBonus;               // 구매 등으로 받은 힌트
        public bool NoAds;                  // 광고 제거 구매

        public string ToJson()
        {
            return MiniJson.Serialize(new Dictionary<string, object>
            {
                { "v", 1 }, { "play", InstallPlayMs }, { "last", LastAdAtMs }, { "since", LevelsSinceInterstitial },
                { "day", Day }, { "dayCount", DayCount }, { "quit", QuitAfterAdCount }, { "total", InterstitialsTotal },
                { "hints", HintsUsed }, { "hintBonus", HintBonus }, { "noAds", NoAds },
            });
        }

        public static AdState FromJson(string json)
        {
            var s = new AdState();
            try
            {
                var r = string.IsNullOrEmpty(json) ? null : MiniJson.Parse(json) as Dictionary<string, object>;
                if (r == null) return s;
                s.InstallPlayMs = Long(r, "play", 0);
                s.LastAdAtMs = Long(r, "last", -1);
                s.LevelsSinceInterstitial = (int)Long(r, "since", 999);
                object v;
                if (r.TryGetValue("day", out v) && v is string) s.Day = (string)v;
                s.DayCount = (int)Long(r, "dayCount", 0);
                s.QuitAfterAdCount = (int)Long(r, "quit", 0);
                s.InterstitialsTotal = (int)Long(r, "total", 0);
                s.HintsUsed = (int)Long(r, "hints", 0);
                s.HintBonus = (int)Long(r, "hintBonus", 0);
                if (r.TryGetValue("noAds", out v) && v is bool) s.NoAds = (bool)v;
            }
            catch (Exception) { }
            return s;
        }

        static long Long(Dictionary<string, object> r, string key, long def)
        {
            object v;
            if (!r.TryGetValue(key, out v)) return def;
            if (v is double) return (long)Math.Round((double)v);
            if (v is long) return (long)v;
            if (v is int) return (int)v;
            return def;
        }
    }

    /// <summary>한 판을 깬 순간의 상황. 시계와 날짜는 밖에서 넣어 결정론적으로 판단한다.</summary>
    public struct AdContext
    {
        public int LevelNumber;           // 방금 깬 레벨 (1부터)
        public int FailsInLevel;          // 이 레벨에서 이번에 깨기 전까지 실패한 횟수
        public int LevelsThisSession;     // 이번 세션에 깬 판 수 (방금 깬 판 포함)
        public int InterstitialsThisSession;
        public long NowMs;
        public string Today;
    }

    public enum Offer { Free, Ad, None }

    /// <summary>
    /// 광고 판단. 전면 광고는 '다음 레벨'을 누를 때 한 번 물어본다.
    /// Decide가 null이면 보여 주고, 아니면 건너뛴 이유(분석용 짧은 이름)를 돌려준다.
    /// </summary>
    public static class AdPolicy
    {
        public static string Decide(AdConfig c, AdState s, AdContext x)
        {
            if (s.NoAds) return "no_ads";
            if (!c.InterstitialEnabled) return "disabled";
            if (x.LevelNumber < c.MinLevel) return "new_user_level";
            if (s.InstallPlayMs < c.MinInstallPlaySeconds * 1000L) return "new_user_time";
            if (c.SkipFirstLevelOfSession && x.LevelsThisSession <= 1) return "first_of_session";
            if (c.SkipAfterFailsInLevel > 0 && x.FailsInLevel >= c.SkipAfterFailsInLevel) return "hard_win";
            if (s.LevelsSinceInterstitial < c.MinLevelsSinceLastAd) return "level_gap";
            if (s.LastAdAtMs >= 0 && x.NowMs - s.LastAdAtMs < IntervalMs(c, s)) return "time_gap";
            if (x.InterstitialsThisSession >= c.SessionCap) return "session_cap";
            if (DayCount(s, x.Today) >= c.DailyCap) return "daily_cap";
            return null;
        }

        /// <summary>광고 뒤 바로 나간 일이 쌓이면 간격을 2배로.</summary>
        public static long IntervalMs(AdConfig c, AdState s)
        {
            long ms = c.MinSecondsSinceLastAd * 1000L;
            return s.QuitAfterAdCount >= c.QuitBackoffAfter ? ms * 2 : ms;
        }

        public static int DayCount(AdState s, string today) { return s.Day == today ? s.DayCount : 0; }

        /// <summary>판을 깰 때마다 (광고를 보든 안 보든) 한 번.</summary>
        public static void OnLevelComplete(AdState s)
        {
            if (s.LevelsSinceInterstitial < 999) s.LevelsSinceInterstitial++;
        }

        public static void OnInterstitialShown(AdState s, long nowMs, string today)
        {
            if (s.Day != today) { s.Day = today; s.DayCount = 0; }
            s.DayCount++;
            s.InterstitialsTotal++;
            s.LevelsSinceInterstitial = 0;
            s.LastAdAtMs = nowMs;
        }

        /// <summary>보상형을 끝까지 봤을 때. 전면 광고 시간 간격도 여기서부터 다시 센다.</summary>
        public static void OnRewardedShown(AdState s, long nowMs) { s.LastAdAtMs = nowMs; }

        /// <summary>광고가 끝나고 나서 앱을 나갔을 때. 광고 탓으로 볼 수 있으면 true.</summary>
        public static bool OnAppLeft(AdConfig c, AdState s, long nowMs)
        {
            if (s.LastAdAtMs < 0 || nowMs - s.LastAdAtMs > c.QuitAfterAdSeconds * 1000L) return false;
            s.QuitAfterAdCount++;
            return true;
        }

        // ---------------- 보상형 ----------------

        /// <summary>실패 창의 이어하기. 1~10레벨은 무료, 그 뒤 광고로 판당 2번까지.</summary>
        public static Offer ContinueOffer(AdConfig c, int levelNumber, int continuesUsed)
        {
            if (levelNumber <= c.FreeContinueUntilLevel) return Offer.Free;
            return continuesUsed < c.ContinuesPerGame ? Offer.Ad : Offer.None;
        }

        /// <summary>남은 무료 힌트 (평생 무료 + 보너스).</summary>
        public static int FreeHintsLeft(AdConfig c, AdState s)
        {
            return Math.Max(0, c.FreeHints + s.HintBonus - s.HintsUsed);
        }

        public static Offer HintOffer(AdConfig c, AdState s)
        {
            return FreeHintsLeft(c, s) > 0 ? Offer.Free : Offer.Ad;
        }

        /// <summary>광고 제거를 샀을 때: 전면 광고 영구 제거 + 힌트.</summary>
        public static void OnNoAdsPurchased(AdConfig c, AdState s)
        {
            if (s.NoAds) return;
            s.NoAds = true;
            s.HintBonus += c.NoAdsHintBonus;
        }
    }

    /// <summary>실험군 배정. 설치 id로 정해져 앱을 다시 켜도 같은 군.</summary>
    public static class Experiment
    {
        /// <summary>weights는 Groups 순서의 비율 (합이 0이면 기본군).</summary>
        public static string Assign(string installId, string experiment, int[] weights, string[] groups)
        {
            int total = 0;
            foreach (var w in weights) total += Math.Max(0, w);
            if (total <= 0) return AdConfig.Base;
            uint h = Fnv1a(experiment + ":" + installId);
            int roll = (int)(h % (uint)total);
            for (int i = 0; i < groups.Length && i < weights.Length; i++)
            {
                roll -= Math.Max(0, weights[i]);
                if (roll < 0) return groups[i];
            }
            return groups[groups.Length - 1];
        }

        public static uint Fnv1a(string s)
        {
            uint h = 2166136261;
            foreach (char ch in s)
            {
                h ^= ch;
                h *= 16777619;
            }
            return h;
        }
    }
}
