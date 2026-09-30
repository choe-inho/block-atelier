using System.Collections.Generic;
using System.IO;
using BlockAtelier.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAtelier.Game
{
    /// <summary>
    /// 광고·분석·원격 설정 연결. 규칙은 Core/AdPolicy, 여기는 버튼과 화면과 기록만.
    /// 원칙(block-atelier-monetization): 플레이 도중 광고 없음, 전면 광고는 '다음 레벨'에서만,
    /// 보상형은 사람이 눌러서 고른다.
    /// </summary>
    public sealed partial class GameRoot
    {
        IAdService ads;
        IAnalytics analytics;
        IRemoteConfig remote;
        AdConfig adConfig = new AdConfig();
        AdState adState = new AdState();
        string installId = "";

        const string AdStateKey = "ba_ads", InstallKey = "ba_install", ExperimentName = "ads1";
        const float NewSessionAfterSeconds = 30 * 60;

        // 세션 (앱을 켜거나 30분 넘게 나갔다 오면 새 세션)
        int sessionLevels, sessionInterstitials;
        float sessionSeconds;
        long pausedAtMs = -1;

        // 판
        int failLevel = -1, failsInLevel, winFails, hintsThisGame;
        bool undoOffered, hintOffered;
        float playSaveTimer;

        GameObject undoAdBadge, hintAdBadge;

        // 가짜 광고 화면
        public static float FakeAdSeconds = 2f;
        bool adOpen, adPending;
        Transform adRoot;
        Canvas adCanvas;
        readonly List<WorldButton> adButtons = new List<WorldButton>();
        float adTimer;
        bool adRewarded, adReady;
        string adPlacement;
        System.Action<bool> adDone;
        Text adCountdown;

        static long NowMs() { return System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }
        static string Today() { return System.DateTime.Now.ToString("yyyy-MM-dd"); }

        void InitMonetization()
        {
            installId = PlayerPrefs.GetString(InstallKey, "");
            if (string.IsNullOrEmpty(installId))
            {
                installId = System.Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(InstallKey, installId);
            }
            adState = AdState.FromJson(PlayerPrefs.GetString(AdStateKey, ""));

            if (remote == null) remote = new LocalRemoteConfig();
#if UNITY_EDITOR
            if (analytics == null) analytics = new DevAnalytics(Path.Combine(LogsDir, "claude_events.txt"));
#else
            if (analytics == null) analytics = new DevAnalytics(null);
#endif
            if (ads == null) ads = new FakeAdService(ShowFakeAd);
            ApplyRemoteConfig();

            sessionLevels = sessionInterstitials = 0;
            sessionSeconds = 0f;
            Log("session_start", "levels_cleared", progress.ClearedCount, "stars", progress.TotalStars, "group", adConfig.Group);
        }

        /// <summary>실험군 배정 → 그 군의 기본값 → 원격 설정 덮어쓰기.</summary>
        void ApplyRemoteConfig()
        {
            // 소프트 런칭 전에는 모두 기본군. 실험 비율은 원격 설정 "ads_weights" = "대조,기본,적극" (예: "1,1,1").
            int[] weights = ParseWeights(remote.Get("ads_weights", "0,1,0"));
            string group = remote.Get("ads_group_force", "");
            if (System.Array.IndexOf(AdConfig.Groups, group) < 0)
                group = Experiment.Assign(installId, ExperimentName, weights, AdConfig.Groups);
            adConfig = AdConfig.ForGroup(group).WithOverrides(remote.Get("ads_config", ""));
            if (analytics != null) analytics.SetUserProperty("ad_group", adConfig.Group);
        }

        static int[] ParseWeights(string s)
        {
            var parts = s.Split(',');
            var w = new int[AdConfig.Groups.Length];
            for (int i = 0; i < w.Length && i < parts.Length; i++) int.TryParse(parts[i].Trim(), out w[i]);
            return w;
        }

        void SaveAds()
        {
            PlayerPrefs.SetString(AdStateKey, adState.ToJson());
            PlayerPrefs.Save();
        }

        void Log(string name, params object[] kv)
        {
            if (analytics == null) return;
            var p = new Dictionary<string, object>();
            for (int i = 0; i + 1 < kv.Length; i += 2) p[(string)kv[i]] = kv[i + 1];
            analytics.Log(name, p);
        }

        // ---------------- 판 흐름 ----------------

        void OnLevelStarted(int index)
        {
            if (index != failLevel) { failLevel = index; failsInLevel = 0; }
            hintsThisGame = 0;
            undoOffered = hintOffered = false;
            Log("level_start", "level", index + 1, "attempt", failsInLevel + 1);
        }

        void OnLevelWon(SubmitResult result)
        {
            winFails = failsInLevel;
            failsInLevel = 0;
            sessionLevels++;
            AdPolicy.OnLevelComplete(adState);
            SaveAds();
            Log("level_complete", "level", levelIndex + 1, "stars", result.Stars, "moves", game.MovesUsed,
                "time_ms", game.ContinuesUsed > 0 ? -1L : clock.ElapsedMs, "continues", game.ContinuesUsed,
                "hints", hintsThisGame, "fails_before", winFails);
        }

        void OnLevelLost()
        {
            failsInLevel++;
            int total = game.Picture.TotalPixels;
            int pct = Mathf.RoundToInt((1f - game.Picture.RemainingPixels / (float)total) * 100f);
            Log("level_fail", "level", levelIndex + 1, "moves", game.MovesUsed, "percent", pct, "continues", game.ContinuesUsed);
        }

        /// <summary>판을 끝내지 않고 홈이나 처음부터로 나갈 때 (어디서 그만두는지 보려고).</summary>
        void LogLeave(string to)
        {
            if (game == null || game.State != GameState.Playing || game.MovesUsed == 0) return;
            int total = game.Picture.TotalPixels;
            int pct = Mathf.RoundToInt((1f - game.Picture.RemainingPixels / (float)total) * 100f);
            Log("level_leave", "level", levelIndex + 1, "moves", game.MovesUsed, "percent", pct, "to", to);
        }

        /// <summary>완성 창의 '다음 레벨'. 조건이 맞으면 1초 안내 뒤 전면 광고, 끝나면 다음 레벨.</summary>
        void NextLevelWithAd()
        {
            if (adPending || adOpen) return;
            int next = levelIndex + 1;
            var ctx = new AdContext
            {
                LevelNumber = levelIndex + 1, FailsInLevel = winFails, LevelsThisSession = sessionLevels,
                InterstitialsThisSession = sessionInterstitials, NowMs = NowMs(), Today = Today(),
            };
            string skip = AdPolicy.Decide(adConfig, adState, ctx);
            if (skip == null && !ads.IsInterstitialReady("next_level")) skip = "no_fill";
            Log("ad_gate", "placement", "next_level", "result", skip ?? "show", "level", levelIndex + 1);
            if (skip != null) { StartLevel(next); return; }

            adPending = true;
            ShowToast("잠깐 광고 후 다음 그림이 열려요");
            Tween.After(1f, () =>
            {
                adPending = false;
                Log("ad_shown", "type", "interstitial", "placement", "next_level", "level", levelIndex + 1);
                ads.ShowInterstitial("next_level", () =>
                {
                    sessionInterstitials++;
                    AdPolicy.OnInterstitialShown(adState, NowMs(), Today());
                    SaveAds();
                    StartLevel(next);
                });
            });
        }

        /// <summary>보상형 공통: 광고를 끝까지 보면 onReward.</summary>
        void ShowRewardedFor(string placement, System.Action onReward)
        {
            if (adOpen || adPending) return;
            if (!ads.IsRewardedReady(placement))
            {
                Log("ad_unavailable", "placement", placement);
                ShowToast("지금은 광고를 불러올 수 없어요");
                return;
            }
            Log("ad_shown", "type", "rewarded", "placement", placement, "level", levelIndex + 1);
            ads.ShowRewarded(placement, ok =>
            {
                if (ok)
                {
                    AdPolicy.OnRewardedShown(adState, NowMs());
                    SaveAds();
                    Log("ad_reward", "placement", placement, "level", levelIndex + 1);
                    onReward();
                }
                else Log("ad_closed", "placement", placement, "rewarded", false);
                RenderHud();
            });
        }

        void OfferShown(string placement)
        {
            Log("ad_offer", "placement", placement, "level", levelIndex + 1);
        }

        // ---------------- 힌트·되돌리기 버튼 ----------------

        void DoHint()
        {
            if (busy || adOpen || adPending || game.State != GameState.Playing) return;
            if (AdPolicy.HintOffer(adConfig, adState) == Offer.Free) UseHint(false);
            else ShowRewardedFor("hint", () => UseHint(true));
        }

        void UseHint(bool viaAd)
        {
            if (!ShowHand(true)) { ShowToast("지금은 보여 줄 수가 없어요"); return; }
            if (!viaAd) adState.HintsUsed++;
            hintsThisGame++;
            SaveAds();
            Log("hint_used", "level", levelIndex + 1, "free", !viaAd, "moves", game.MovesUsed);
            RenderHud();
        }

        WorldButton BottomButton(string text, float x)
        {
            var b = new WorldButton(bottom, bottomCanvas, text, new Vector2(x, 0.85f), new Vector2(2.35f, 0.95f), false, 45);
            b.Label.fontSize = 32;
            return b;
        }

        /// <summary>버튼 오른쪽 위의 작은 '광고' 표시.</summary>
        static GameObject AdBadge(Transform parent, Canvas canvas, Vector2 pos, int order)
        {
            var root = new GameObject("AdBadge");
            root.transform.SetParent(parent, false);
            var bg = UI.Panel("AdBadgeBg", root.transform, pos, new Vector2(0.62f, 0.34f), UI.Accent, order);
            var t = UI.Label(canvas, "광고", pos, 0.19f, UI.ButtonPriInk, TextAnchor.MiddleCenter, 0.62f, true);
            // 글자는 캔버스 밑에 있으니 배지를 켜고 끌 때 같이 움직이도록 따로 붙잡아 둔다
            var follow = root.AddComponent<BadgeLink>();
            follow.Label = t.gameObject;
            return root;
        }

        void RenderAdButtons()
        {
            bool playing = !busy && game.State == GameState.Playing;
            bool undoAd = undoLeft <= 0;
            undoBtn.Label.text = undoAd ? "되돌리기 +" + adConfig.UndoRefill : "되돌리기 " + undoLeft;
            undoBtn.SetEnabled(playing && game.CanUndo);
            SetBadge(undoAdBadge, undoAd && game.CanUndo);
            if (undoAd && game.CanUndo && !undoOffered) { undoOffered = true; OfferShown("undo"); }

            int free = AdPolicy.FreeHintsLeft(adConfig, adState);
            hintBtn.Label.text = free > 0 ? "힌트 " + free : "힌트";
            hintBtn.SetEnabled(playing);
            SetBadge(hintAdBadge, free <= 0);
            if (free <= 0 && playing && !hintOffered) { hintOffered = true; OfferShown("hint"); }
        }

        static void SetBadge(GameObject badge, bool on)
        {
            if (badge != null && badge.activeSelf != on) badge.SetActive(on);
        }

        // ---------------- 시간·세션 ----------------

        void TickMonetization(float dt)
        {
            if (adOpen) TickFakeAd(dt);
            if (!Application.isFocused) return;
            sessionSeconds += dt;
            playSaveTimer += dt;
            if (playSaveTimer >= 5f)
            {
                adState.InstallPlayMs += (long)(playSaveTimer * 1000f);
                playSaveTimer = 0f;
                SaveAds();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (analytics == null) return;
            long now = NowMs();
            if (paused)
            {
                pausedAtMs = now;
                adState.InstallPlayMs += (long)(playSaveTimer * 1000f);
                playSaveTimer = 0f;
                if (AdPolicy.OnAppLeft(adConfig, adState, now))
                    Log("ad_quit", "seconds_after_ad", (now - adState.LastAdAtMs) / 1000f, "level", levelIndex + 1);
                SaveAds();
                Log("session_end", "seconds", Mathf.RoundToInt(sessionSeconds), "levels", sessionLevels, "ads", sessionInterstitials);
            }
            else if (pausedAtMs >= 0 && now - pausedAtMs > NewSessionAfterSeconds * 1000f)
            {
                sessionLevels = sessionInterstitials = 0;
                sessionSeconds = 0f;
                Log("session_start", "levels_cleared", progress.ClearedCount, "stars", progress.TotalStars, "group", adConfig.Group);
            }
        }

        void OnApplicationQuit()
        {
            OnApplicationPause(true);
        }

        // ---------------- 가짜 광고 화면 ----------------

        void ShowFakeAd(bool rewarded, string placement, System.Action<bool> done)
        {
            CancelDrag();
            adOpen = true;
            adRewarded = rewarded;
            adPlacement = placement;
            adDone = done;
            adTimer = FakeAdSeconds;
            adReady = false;
            adButtons.Clear();

            adRoot = new GameObject("FakeAd").transform;
            adRoot.SetParent(transform, false);
            var cp = cam.transform.position;
            adRoot.position = new Vector3(cp.x, cp.y, 0f);
            float size = cam.orthographicSize;
            float s = Mathf.Min(1f, size * cam.aspect * 2f * 0.9f / 8.6f, size * 2f * 0.8f / 9f);
            adRoot.localScale = Vector3.one * s;

            var dim = Gfx.MakeSprite("AdDim", adRoot, Gfx.Pixel, new Color(0.02f, 0.02f, 0.05f, 0.96f), 95);
            dim.transform.localScale = Vector3.one * 400f;   // 화면이 돌거나 펼쳐져도 다 덮도록 넉넉하게
            UI.Panel("AdCard", adRoot, Vector2.zero, new Vector2(8.6f, 9f), Gfx.Hex("#F4F1EA"), 96);
            adCanvas = UI.WorldCanvas("AdText", adRoot, 99);
            UI.Label(adCanvas, "테스트 광고", new Vector2(0f, 3.2f), 0.7f, Gfx.Hex("#2A2340"), TextAnchor.MiddleCenter, 8f, true);
            UI.Label(adCanvas, (rewarded ? "보상형" : "전면") + "  ·  " + placement, new Vector2(0f, 2.3f), 0.3f, Gfx.Hex("#6B6480"));
            UI.Label(adCanvas, "실제 광고 SDK를 붙이면 이 자리에 광고가 나와요", new Vector2(0f, 1.2f), 0.26f, Gfx.Hex("#6B6480"), TextAnchor.MiddleCenter, 8f);
            adCountdown = UI.Label(adCanvas, "", new Vector2(0f, 0f), 0.42f, Gfx.Hex("#2A2340"), TextAnchor.MiddleCenter, 8f, true);
            if (rewarded)
            {
                // 보상형은 언제든 건너뛸 수 있다 (보상 없음)
                var skip = new WorldButton(adRoot, adCanvas, "건너뛰기", new Vector2(0f, -3.3f), new Vector2(6.6f, 1.0f), false, 97);
                skip.OnClick = () => CloseFakeAd(false);
                adButtons.Add(skip);
            }
            UpdateAdCountdown();
        }

        void TickFakeAd(float dt)
        {
            if (adReady) return;
            adTimer -= dt;
            UpdateAdCountdown();
            if (adTimer > 0f) return;
            adReady = true;
            var b = new WorldButton(adRoot, adCanvas, adRewarded ? "보상 받기" : "닫기", new Vector2(0f, -2.0f), new Vector2(6.6f, 1.1f), true, 97);
            b.OnClick = () => CloseFakeAd(adRewarded);
            adButtons.Add(b);
        }

        void UpdateAdCountdown()
        {
            if (adCountdown == null) return;
            adCountdown.text = adReady ? (adRewarded ? "끝까지 봤어요" : "") : Mathf.CeilToInt(Mathf.Max(0f, adTimer)) + "초";
        }

        void CloseFakeAd(bool rewarded)
        {
            if (!adOpen) return;
            adOpen = false;
            foreach (var b in adButtons) b.SetVisible(false);
            adButtons.Clear();
            if (adRoot != null)
            {
                adRoot.gameObject.SetActive(false);
                Destroy(adRoot.gameObject);
            }
            adRoot = null;
            adCountdown = null;
            var done = adDone;
            adDone = null;
            if (done != null) done(rewarded);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 개발 명령: ads(상태), adgroup 군, adweights 1,1,1, adconfig {json}, adtime 초, adfill on|off,
        /// playtime 초(설치 후 누적 플레이), noads, adreset.
        /// </summary>
        bool DevAdCommand(string[] parts)
        {
            string arg = parts.Length > 1 ? string.Join(" ", parts, 1, parts.Length - 1) : "";
            switch (parts[0])
            {
                case "ads":
                    Ack("group " + adConfig.Group + " session " + sessionLevels + "/" + sessionInterstitials + " fails " + failsInLevel
                        + " undo " + undoLeft + " hintsFree " + AdPolicy.FreeHintsLeft(adConfig, adState) + " state " + adState.ToJson());
                    return true;
                case "adgroup":
                    LocalRemoteConfig.SetOverride("ads_group_force", arg);
                    ApplyRemoteConfig();
                    return true;
                case "adweights":
                    LocalRemoteConfig.SetOverride("ads_weights", arg);
                    ApplyRemoteConfig();
                    return true;
                case "adconfig":
                    LocalRemoteConfig.SetOverride("ads_config", arg);
                    ApplyRemoteConfig();
                    return true;
                case "adtime":
                    FakeAdSeconds = float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                case "adfill":
                    FakeAdService.Fill = arg != "off";
                    return true;
                case "playtime":
                    adState.InstallPlayMs = long.Parse(arg) * 1000L;
                    SaveAds();
                    return true;
                case "noads":
                    AdPolicy.OnNoAdsPurchased(adConfig, adState);
                    SaveAds();
                    RenderHud();
                    return true;
                case "adreset":
                    adState = new AdState();
                    SaveAds();
                    sessionLevels = sessionInterstitials = 0;
                    RenderHud();
                    return true;
                case "loseview":
                    // 실패 창만 띄워 본다 (이어하기 제안 확인용, 판 상태는 그대로)
                    StartCoroutine(LoseSequence());
                    return true;
                case "session":
                    // session N : 이번 세션에 깬 판 수를 맞춘다 (세션 첫 판 규칙 확인용)
                    sessionLevels = int.Parse(arg);
                    return true;
            }
            return false;
        }
#endif
    }

    /// <summary>배지를 끄고 켤 때 캔버스 쪽 글자도 같이.</summary>
    public sealed class BadgeLink : MonoBehaviour
    {
        public GameObject Label;
        void OnEnable() { if (Label != null) Label.SetActive(true); }
        void OnDisable() { if (Label != null) Label.SetActive(false); }
        void OnDestroy() { if (Label != null) Destroy(Label); }
    }
}
