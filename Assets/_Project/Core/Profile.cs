using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BlockAtelier.Core
{
    /// <summary>
    /// 닉네임 규칙. 랭킹에 보이므로 서버도 같은 규칙으로 한 번 더 검사한다.
    /// 2~10자, 한글 완성형·영문·숫자만 (공백·기호 불가), 금칙어 불가.
    /// </summary>
    public static class Nickname
    {
        public const int MinLength = 2, MaxLength = 10;

        // 최소한의 금칙어. 실제 목록은 서버에서 관리하고 여기선 흔한 것만 먼저 막는다.
        static readonly string[] Banned = { "운영자", "관리자", "admin", "gm", "시발", "씨발", "병신", "개새", "좆", "섹스", "fuck", "shit" };

        static readonly string[] Adjectives =
        {
            "용감한", "졸린", "반짝이는", "느긋한", "배고픈", "수줍은", "씩씩한", "몽글한",
            "새침한", "다정한", "엉뚱한", "포근한", "재빠른", "꼼꼼한", "행복한", "조용한",
        };

        static readonly string[] Nouns =
        {
            "판다", "고래", "토끼", "펭귄", "여우", "병아리", "문어", "복어",
            "딸기", "수박", "도넛", "푸딩", "해바라기", "선인장", "구름", "호랑이",
        };

        public static string Normalize(string s) { return s == null ? "" : s.Trim(); }

        /// <summary>문제가 없으면 null, 있으면 사용자에게 보여 줄 한 줄.</summary>
        public static string Validate(string raw)
        {
            string s = Normalize(raw);
            int len = new StringInfo(s).LengthInTextElements;
            if (len < MinLength) return "2자 이상 입력해 주세요";
            if (len > MaxLength) return "10자까지 쓸 수 있어요";
            foreach (char c in s)
            {
                bool hangul = c >= '가' && c <= '힣';
                bool latin = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
                bool digit = c >= '0' && c <= '9';
                if (!hangul && !latin && !digit) return "한글, 영문, 숫자만 쓸 수 있어요";
            }
            string lower = s.ToLowerInvariant();
            foreach (var b in Banned)
                if (lower.Contains(b)) return "쓸 수 없는 단어가 들어 있어요";
            return null;
        }

        /// <summary>그림 이름으로 만든 자동 닉네임. 같은 seed면 같은 이름 (8자 이하라 항상 규칙 통과).</summary>
        public static string Suggest(int seed)
        {
            var rng = new Random(seed);
            return Adjectives[rng.Next(Adjectives.Length)] + Nouns[rng.Next(Nouns.Length)];
        }
    }

    /// <summary>마이페이지 정보: 닉네임과 대표 그림(레벨 번호, 0이면 아직 없음).</summary>
    public sealed class PlayerProfile
    {
        public string Nickname = "";
        public int AvatarLevelId;

        public static PlayerProfile CreateGuest(int seed)
        {
            return new PlayerProfile { Nickname = Core.Nickname.Suggest(seed), AvatarLevelId = 0 };
        }

        /// <summary>규칙에 맞으면 바꾸고 null, 아니면 이유를 돌려주고 그대로 둔다.</summary>
        public string TrySetNickname(string raw)
        {
            string err = Core.Nickname.Validate(raw);
            if (err == null) Nickname = Core.Nickname.Normalize(raw);
            return err;
        }

        public string ToJson()
        {
            return MiniJson.Serialize(new Dictionary<string, object> { { "v", 1 }, { "nickname", Nickname }, { "avatar", AvatarLevelId } });
        }

        public static PlayerProfile FromJson(string json, int seedIfNew)
        {
            try
            {
                var root = string.IsNullOrEmpty(json) ? null : MiniJson.Parse(json) as Dictionary<string, object>;
                if (root != null)
                {
                    var p = new PlayerProfile();
                    object v;
                    if (root.TryGetValue("nickname", out v) && v is string && Core.Nickname.Validate((string)v) == null) p.Nickname = (string)v;
                    else p.Nickname = Core.Nickname.Suggest(seedIfNew);
                    if (root.TryGetValue("avatar", out v) && v != null) p.AvatarLevelId = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                    return p;
                }
            }
            catch (Exception) { }
            return CreateGuest(seedIfNew);
        }
    }

    /// <summary>설정: 소리, 효과 줄이기(화면 흔들림·번쩍임 끔).</summary>
    public sealed class GameSettings
    {
        public bool Sound = true;
        public bool ReduceMotion;

        public string ToJson()
        {
            return MiniJson.Serialize(new Dictionary<string, object> { { "v", 1 }, { "sound", Sound }, { "reduceMotion", ReduceMotion } });
        }

        public static GameSettings FromJson(string json)
        {
            var s = new GameSettings();
            try
            {
                var root = string.IsNullOrEmpty(json) ? null : MiniJson.Parse(json) as Dictionary<string, object>;
                if (root == null) return s;
                object v;
                if (root.TryGetValue("sound", out v) && v is bool) s.Sound = (bool)v;
                if (root.TryGetValue("reduceMotion", out v) && v is bool) s.ReduceMotion = (bool)v;
            }
            catch (Exception) { }
            return s;
        }
    }

    /// <summary>앨범 하나의 진행 요약 (홈 책장 카드용).</summary>
    public struct AlbumSummary
    {
        public string Key, Title;
        public int FirstIndex, Count, Cleared, Stars;
        public bool Unlocked;
        /// <summary>표지로 쓸 레벨: 가장 최근에 완성한 그림, 없으면 첫 그림.</summary>
        public int CoverIndex;
        public bool CoverCleared;
        public bool Complete { get { return Count > 0 && Cleared == Count; } }

        /// <summary>레벨 목록을 앨범별로 묶는다 (같은 album 키가 이어진 구간 = 한 앨범).</summary>
        public static List<AlbumSummary> Build(IList<LevelData> levels, Progress progress)
        {
            var list = new List<AlbumSummary>();
            int i = 0;
            while (i < levels.Count)
            {
                var a = new AlbumSummary { Key = levels[i].Album, Title = levels[i].AlbumTitle, FirstIndex = i, CoverIndex = i };
                int j = i;
                while (j < levels.Count && levels[j].Album == a.Key)
                {
                    var r = progress.Get(levels[j].Id);
                    a.Count++;
                    a.Stars += r.Stars;
                    if (r.Cleared) { a.Cleared++; a.CoverIndex = j; a.CoverCleared = true; }
                    j++;
                }
                a.Unlocked = progress.IsUnlocked(levels[i].Id);
                if (string.IsNullOrEmpty(a.Title)) a.Title = "앨범 " + (list.Count + 1);
                list.Add(a);
                i = j;
            }
            return list;
        }
    }
}
