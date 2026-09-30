// 광고 빈도 시뮬레이션: 실험군별로 '플레이 1시간에 전면 광고 몇 번'인지 본다.
// balance_report.tsv의 레벨별 클리어율·평균 수로 가상 플레이어 여러 명을 7일 동안 돌린다.
//
//   mcs -langversion:7.2 -optimize+ -out:AdSim.exe $(find Assets/_Project/Core -name '*.cs') Tools/LevelTool/AdSim.cs
//   mono AdSim.exe Tools/LevelTool/balance_report.tsv [플레이어 수]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BlockAtelier.Core;

static class AdSim
{
    // 플레이어 모델 (사람 테스트가 생기면 이 값부터 고친다)
    const double SecondsPerMove = 3.5;      // 한 수 생각 + 끌어 놓기 + 연출
    const double WinOverhead = 12;          // 완성 연출 + 완성 창
    const double FailOverhead = 7;          // 실패 창
    const double AdSeconds = 30;            // 광고 한 편 (전면·보상형)
    const double ContinueByAdChance = 0.35; // 광고 이어하기를 고르는 비율
    const double ContinueWinChance = 0.6;   // 이어하기 뒤 깨는 비율
    const int SessionsPerDay = 3, Days = 7;

    struct Lv { public double Clear, Moves; }

    static int Main(string[] args)
    {
        var levels = Load(args[0]);
        int players = args.Length > 1 ? int.Parse(args[1]) : 300;
        var sb = new StringBuilder();
        sb.AppendLine("group\tplayHours\tinterPerHour\tinterPerSession\tinterPerLevel\trewardedPerHour\tfirstAdMin\tfirstAdLevel\tlevelsDone\tsessionCapHit\tdailyCapHit");
        Console.WriteLine("군          1시간당 전면  세션당  레벨당   1시간당 보상형  첫 광고(분·레벨)  7일 레벨  세션상한  하루상한");
        foreach (var group in AdConfig.Groups)
        {
            var c = AdConfig.ForGroup(group);
            var tot = new Totals();
            for (int p = 0; p < players; p++) RunPlayer(c, levels, new Random(1000 + p), tot);
            double hours = tot.PlaySec / 3600.0;
            double firstMin = tot.FirstAdCount > 0 ? tot.FirstAdMinSum / tot.FirstAdCount : -1;
            double firstLv = tot.FirstAdCount > 0 ? tot.FirstAdLevelSum / (double)tot.FirstAdCount : -1;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0,-11} {1,8:0.00} {2,9:0.00} {3,7:0.000} {4,12:0.00} {5,10:0.0}·{6,4:0.0} {7,9:0.0} {8,8:0.0%} {9,8:0.0%}",
                group, tot.Inter / hours, tot.Inter / (double)tot.Sessions, tot.Inter / (double)tot.Levels, tot.Rewarded / hours,
                firstMin, firstLv, tot.Levels / (double)players, tot.SessionCap / (double)tot.Sessions, tot.DailyCap / (double)(players * Days)));
            sb.AppendLine(string.Join("\t", group, F(hours / players), F(tot.Inter / hours), F(tot.Inter / (double)tot.Sessions),
                F(tot.Inter / (double)tot.Levels), F(tot.Rewarded / hours), F(firstMin), F(firstLv), F(tot.Levels / (double)players),
                F(tot.SessionCap / (double)tot.Sessions), F(tot.DailyCap / (double)(players * Days))));
        }
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0])), "ad_report.tsv"), sb.ToString());
        return 0;
    }

    static string F(double v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }

    sealed class Totals
    {
        public double PlaySec, FirstAdMinSum, FirstAdLevelSum;
        public int Inter, Rewarded, Sessions, Levels, FirstAdCount, SessionCap, DailyCap;
    }

    static void RunPlayer(AdConfig c, List<Lv> levels, Random rng, Totals tot)
    {
        var s = new AdState();
        long now = 0;               // ms, 가상 시계
        int level = 0;              // 다음에 할 레벨 (0부터)
        bool firstAd = false;
        for (int day = 0; day < Days && level < levels.Count; day++)
        {
            string today = "d" + day;
            for (int ses = 0; ses < SessionsPerDay && level < levels.Count; ses++)
            {
                now = (day * 24L + 9 + ses * 5) * 3600 * 1000;
                double sessionLen = (6 + rng.NextDouble() * 14) * 60;     // 6~20분
                double t = 0;
                int sessionLevels = 0, sessionInter = 0, fails = 0;
                bool capHit = false;
                tot.Sessions++;
                while (t < sessionLen && level < levels.Count)
                {
                    var lv = levels[level];
                    double moves = Math.Max(8, lv.Moves * (0.8 + rng.NextDouble() * 0.4));
                    double sec = moves * SecondsPerMove;
                    bool win = rng.NextDouble() < lv.Clear;
                    if (!win && level + 1 > c.FreeContinueUntilLevel && rng.NextDouble() < ContinueByAdChance)
                    {
                        // 광고 보고 이어하기
                        sec += FailOverhead + AdSeconds;
                        tot.Rewarded++;
                        AdPolicy.OnRewardedShown(s, now + (long)((t + sec) * 1000));
                        win = rng.NextDouble() < ContinueWinChance;
                        sec += moves * 0.3 * SecondsPerMove;
                    }
                    else if (!win && level + 1 <= c.FreeContinueUntilLevel) { win = true; sec += FailOverhead + moves * 0.3 * SecondsPerMove; }
                    t += sec;
                    s.InstallPlayMs += (long)(sec * 1000);
                    if (!win)
                    {
                        t += FailOverhead;
                        s.InstallPlayMs += (long)(FailOverhead * 1000);
                        fails++;
                        continue;
                    }
                    t += WinOverhead;
                    s.InstallPlayMs += (long)(WinOverhead * 1000);
                    sessionLevels++;
                    tot.Levels++;
                    AdPolicy.OnLevelComplete(s);
                    var ctx = new AdContext
                    {
                        LevelNumber = level + 1, FailsInLevel = fails, LevelsThisSession = sessionLevels,
                        InterstitialsThisSession = sessionInter, NowMs = now + (long)(t * 1000), Today = today,
                    };
                    string skip = AdPolicy.Decide(c, s, ctx);
                    if (skip == "session_cap") capHit = true;
                    if (skip == "daily_cap") tot.DailyCap++;
                    if (skip == null)
                    {
                        t += AdSeconds;
                        AdPolicy.OnInterstitialShown(s, now + (long)(t * 1000), today);
                        sessionInter++;
                        tot.Inter++;
                        if (!firstAd)
                        {
                            firstAd = true;
                            tot.FirstAdCount++;
                            tot.FirstAdMinSum += s.InstallPlayMs / 60000.0;
                            tot.FirstAdLevelSum += level + 1;
                        }
                    }
                    fails = 0;
                    level++;
                }
                if (capHit) tot.SessionCap++;
                tot.PlaySec += t;
            }
        }
    }

    static List<Lv> Load(string path)
    {
        var list = new List<Lv>();
        var lines = File.ReadAllLines(path);
        var head = lines[0].Split('\t');
        int ci = Array.IndexOf(head, "clearRate"), mi = Array.IndexOf(head, "avgMoves");
        for (int i = 1; i < lines.Length; i++)
        {
            var p = lines[i].Split('\t');
            if (p.Length <= Math.Max(ci, mi)) continue;
            list.Add(new Lv
            {
                Clear = double.Parse(p[ci], CultureInfo.InvariantCulture),
                Moves = double.Parse(p[mi], CultureInfo.InvariantCulture),
            });
        }
        return list;
    }
}
