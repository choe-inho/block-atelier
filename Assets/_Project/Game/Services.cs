using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BlockAtelier.Game
{
    /// <summary>
    /// 광고 SDK 자리. 지금은 FakeAdService(에디터·개발용 가짜 광고),
    /// 출시 때 LevelPlayAdService 하나로 바꿔 끼운다. 콜백은 메인 스레드에서 불러야 한다.
    /// </summary>
    public interface IAdService
    {
        bool IsRewardedReady(string placement);
        bool IsInterstitialReady(string placement);
        /// <summary>끝나면 onDone(보상 받았는지). 광고가 안 뜨면 바로 onDone(false).</summary>
        void ShowRewarded(string placement, Action<bool> onDone);
        void ShowInterstitial(string placement, Action onDone);
    }

    /// <summary>분석 SDK 자리 (출시 때 Firebase Analytics). 이벤트 이름은 소문자_밑줄, 40자 이내.</summary>
    public interface IAnalytics
    {
        void Log(string name, Dictionary<string, object> parameters);
        void SetUserProperty(string name, string value);
    }

    /// <summary>원격 설정 자리 (출시 때 Firebase Remote Config). 값이 없으면 def.</summary>
    public interface IRemoteConfig
    {
        string Get(string key, string def);
    }

    /// <summary>
    /// 개발용 분석: 콘솔과 Logs/claude_events.txt(에디터)에 한 줄씩 남긴다.
    /// 형식: 시각 이름 키=값 ... (나중에 실제 SDK로 바꿔도 부르는 쪽은 그대로)
    /// </summary>
    public sealed class DevAnalytics : IAnalytics
    {
        readonly string path;
        readonly Dictionary<string, string> props = new Dictionary<string, string>();
        public readonly Dictionary<string, int> Counts = new Dictionary<string, int>();

        public DevAnalytics(string logPath) { path = logPath; }

        public void Log(string name, Dictionary<string, object> parameters)
        {
            int n;
            Counts.TryGetValue(name, out n);
            Counts[name] = n + 1;
            var sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("HH:mm:ss.f")).Append(' ').Append(name);
            if (parameters != null)
                foreach (var kv in parameters) sb.Append(' ').Append(kv.Key).Append('=').Append(Format(kv.Value));
            string line = sb.ToString();
            if (Debug.isDebugBuild) Debug.Log("[분석] " + line);
            if (path == null) return;
            try { File.AppendAllText(path, line + "\n"); }
            catch (Exception) { }
        }

        public void SetUserProperty(string name, string value)
        {
            props[name] = value;
            Log("user_property", new Dictionary<string, object> { { name, value } });
        }

        static string Format(object v)
        {
            if (v is bool) return (bool)v ? "1" : "0";
            if (v is float) return ((float)v).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            if (v is double) return ((double)v).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            return v == null ? "" : v.ToString();
        }
    }

    /// <summary>원격 설정이 없을 때: PlayerPrefs의 ba_remote_&lt;키&gt;(개발 명령으로 넣음), 없으면 기본값.</summary>
    public sealed class LocalRemoteConfig : IRemoteConfig
    {
        public string Get(string key, string def)
        {
            string v = PlayerPrefs.GetString("ba_remote_" + key, "");
            return string.IsNullOrEmpty(v) ? def : v;
        }

        public static void SetOverride(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) PlayerPrefs.DeleteKey("ba_remote_" + key);
            else PlayerPrefs.SetString("ba_remote_" + key, value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>에디터·개발용 가짜 광고. 화면은 GameRoot가 그린다 (몇 초 뒤 닫기/보상 버튼).</summary>
    public sealed class FakeAdService : IAdService
    {
        public static bool Fill = true;        // false면 '광고 없음' 상황을 흉내
        readonly Action<bool, string, Action<bool>> show;

        public FakeAdService(Action<bool, string, Action<bool>> show) { this.show = show; }

        public bool IsRewardedReady(string placement) { return Fill; }
        public bool IsInterstitialReady(string placement) { return Fill; }

        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            if (!Fill) { onDone(false); return; }
            show(true, placement, onDone);
        }

        public void ShowInterstitial(string placement, Action onDone)
        {
            if (!Fill) { onDone(); return; }
            show(false, placement, _ => onDone());
        }
    }
}
