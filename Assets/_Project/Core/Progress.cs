using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlockAtelier.Core
{
    /// <summary>레벨 하나의 기록: 최고 별과 최단 시간만 남긴다.</summary>
    public struct LevelRecord
    {
        public int Stars;          // 0이면 아직 못 깸
        public long BestTimeMs;    // -1이면 시간 기록 없음 (이어하기로만 깼거나 미완성)

        public bool Cleared { get { return Stars > 0; } }
        public static LevelRecord Empty { get { return new LevelRecord { Stars = 0, BestTimeMs = -1 }; } }

        /// <summary>둘 중 좋은 쪽만 남긴다 (별은 많은 쪽, 시간은 빠른 쪽). 순서와 무관, 여러 번 해도 같음.</summary>
        public static LevelRecord Best(LevelRecord a, LevelRecord b)
        {
            long t;
            if (a.BestTimeMs < 0) t = b.BestTimeMs;
            else if (b.BestTimeMs < 0) t = a.BestTimeMs;
            else t = Math.Min(a.BestTimeMs, b.BestTimeMs);
            return new LevelRecord { Stars = Math.Max(a.Stars, b.Stars), BestTimeMs = t };
        }
    }

    public sealed class SubmitResult
    {
        public int Stars, PrevStars;
        public long TimeMs, PrevBestMs;
        public bool TimeCounted;
        public bool NewBestTime { get { return TimeCounted && (PrevBestMs < 0 || TimeMs < PrevBestMs); } }
        public bool MoreStars { get { return Stars > PrevStars; } }
        public bool FirstClear { get { return PrevStars == 0; } }
    }

    /// <summary>
    /// 전체 진행 기록. 게스트는 폰에만, 계정을 연결하면 서버와 Merge로 합친다.
    /// 잠금 규칙: 1레벨은 항상 열림, 그 뒤는 앞 레벨을 깨면 열림.
    /// </summary>
    public sealed class Progress
    {
        readonly Dictionary<int, LevelRecord> records = new Dictionary<int, LevelRecord>();

        public LevelRecord Get(int levelId)
        {
            LevelRecord r;
            return records.TryGetValue(levelId, out r) ? r : LevelRecord.Empty;
        }

        public bool IsUnlocked(int levelId) { return levelId <= 1 || Get(levelId - 1).Cleared; }

        public int TotalStars
        {
            get { int s = 0; foreach (var r in records.Values) s += r.Stars; return s; }
        }

        public int ClearedCount
        {
            get { int n = 0; foreach (var r in records.Values) if (r.Cleared) n++; return n; }
        }

        /// <summary>가장 높은 열린 레벨 (이어서 할 레벨)</summary>
        public int FrontierLevel(int maxLevel)
        {
            int id = 1;
            while (id < maxLevel && Get(id).Cleared) id++;
            return id;
        }

        /// <summary>한 판을 완성했을 때. 더 좋은 기록만 남기고, 무엇이 바뀌었는지 돌려준다.</summary>
        public SubmitResult Submit(LevelData lv, int moves, int continuesUsed, long timeMs)
        {
            var prev = Get(lv.Id);
            var res = new SubmitResult
            {
                Stars = Scoring.Stars(lv, moves, continuesUsed),
                PrevStars = prev.Stars,
                TimeMs = timeMs,
                PrevBestMs = prev.BestTimeMs,
                TimeCounted = Scoring.TimeCounts(continuesUsed) && timeMs >= 0,
            };
            var now = new LevelRecord { Stars = res.Stars, BestTimeMs = res.TimeCounted ? timeMs : -1 };
            records[lv.Id] = LevelRecord.Best(prev, now);
            return res;
        }

        public void Set(int levelId, LevelRecord r) { records[levelId] = r; }

        /// <summary>다른 기록(서버, 다른 폰)과 합친다. 레벨마다 좋은 쪽.</summary>
        public void Merge(Progress other)
        {
            foreach (var kv in other.records) records[kv.Key] = LevelRecord.Best(Get(kv.Key), kv.Value);
        }

        public IEnumerable<KeyValuePair<int, LevelRecord>> All { get { return records; } }

        // 저장 형식: {"v":1,"levels":{"1":[3,42370],"2":[2,-1]}}  (짧게, 서버 전송에도 그대로)
        public string ToJson()
        {
            var levels = new Dictionary<string, object>();
            var keys = new List<int>(records.Keys);
            keys.Sort();
            foreach (var k in keys)
            {
                var r = records[k];
                if (!r.Cleared && r.BestTimeMs < 0) continue;
                levels[k.ToString(CultureInfo.InvariantCulture)] = new List<object> { r.Stars, r.BestTimeMs };
            }
            return MiniJson.Serialize(new Dictionary<string, object> { { "v", 1 }, { "levels", levels } });
        }

        public static Progress FromJson(string json)
        {
            var p = new Progress();
            if (string.IsNullOrEmpty(json)) return p;
            Dictionary<string, object> root;
            try { root = MiniJson.Parse(json) as Dictionary<string, object>; }
            catch (Exception) { return p; }   // 깨진 저장은 빈 기록으로 (서버가 있으면 다시 받아 온다)
            object lv;
            if (root == null || !root.TryGetValue("levels", out lv) || !(lv is Dictionary<string, object>)) return p;
            foreach (var kv in (Dictionary<string, object>)lv)
            {
                int id;
                var arr = kv.Value as List<object>;
                if (!int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out id) || arr == null || arr.Count < 2) continue;
                int stars = Math.Max(0, Math.Min(Scoring.MaxStars, Convert.ToInt32(arr[0], CultureInfo.InvariantCulture)));
                long t = Convert.ToInt64(arr[1], CultureInfo.InvariantCulture);
                p.records[id] = new LevelRecord { Stars = stars, BestTimeMs = t < 0 ? -1 : t };
            }
            return p;
        }
    }
}
