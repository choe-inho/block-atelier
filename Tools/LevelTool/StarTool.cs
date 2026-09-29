// 별 기준 보정기: 레벨마다 "몇 수 이하면 별 3개/2개"를 자동 풀이 봇 시뮬레이션으로 정한다.
//
// 목표 (보통 실력으로 깬 판 기준): 별 3개 약 25%, 별 2개 이상 약 65%.
// 1~10레벨은 처음 배우는 구간이라 넉넉하게: 별 3개 45%, 2개 이상 85%.
// 방법 (반복 보정):
//   1) 표본 A로 기준을 맞춘다 (깬 판들의 수 분포에서 목표 비율에 가장 가까운 정수 기준).
//   2) 처음 보는 표본 B로 검증한다. 목표에서 허용 오차 이상 벗어나면
//   3) A와 B를 합쳐 다시 맞추고, 또 새 표본으로 검증한다. 오차 안에 들거나 최대 횟수까지 반복.
//   4) 마지막으로 새 표본 한 번 더 돌려 최종 비율을 보고서에 남긴다 (과적합 확인용).
// 사용: mono StarTool.exe <레벨 폴더> [표본 판 수=200] [시작 레벨] [끝 레벨]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BlockAtelier.Core;
using BlockAtelier.Core.Sim;

static class StarTool
{
    const double Tolerance = 0.07;
    static double Target3 = 0.25, Target2 = 0.65;
    const double HumanNoise = 25;
    const int MaxRounds = 4;

    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        string dir = args.Length > 0 ? args[0] : "Levels";
        int runs = args.Length > 1 ? int.Parse(args[1]) : 200;
        int from = args.Length > 2 ? int.Parse(args[2]) : 1;
        int to = args.Length > 3 ? int.Parse(args[3]) : 999;

        var report = new StringBuilder();
        report.AppendLine("level\tpicture\tblocks\tgreedyMoves\tminMoves\tmedianMoves\tstar3\tstar2\trounds\twinRate\tfinal3\tfinal2");
        for (int id = from; id <= to; id++)
        {
            string path = Path.Combine(dir, "level_" + id.ToString("000") + ".json");
            if (!File.Exists(path)) break;
            string json = File.ReadAllText(path, Encoding.UTF8);
            var lv = LevelData.FromJson(json);
            if (id <= 10) { Target3 = 0.45; Target2 = 0.85; } else { Target3 = 0.25; Target2 = 0.65; }

            int round = 0, seedBase = 1;
            var pool = new List<int>();
            int wins = 0, total = 0;
            int t3 = 0, t2 = 0;
            while (true)
            {
                var sample = WinMoves(lv, runs, seedBase++, ref wins, ref total);
                if (round > 0)
                {
                    // 검증: 지금 기준이 처음 보는 표본에서도 목표 비율을 내는가
                    double r3 = Share(sample, t3), r2 = Share(sample, t2);
                    if ((Math.Abs(r3 - Target3) <= Tolerance && Math.Abs(r2 - Target2) <= Tolerance) || round >= MaxRounds)
                    {
                        pool.AddRange(sample);
                        break;
                    }
                }
                pool.AddRange(sample);
                Fit(pool, out t3, out t2);
                round++;
            }
            Fit(pool, out t3, out t2);   // 모인 표본 전부로 최종 기준

            // 최종 확인: 보정에 쓰지 않은 새 표본
            int w2 = 0, n2 = 0;
            var fresh = WinMoves(lv, runs, 1000 + id, ref w2, ref n2);
            double f3 = Share(fresh, t3), f2 = Share(fresh, t2);

            var g = new GameSession(lv);
            new AutoSolver(1, 0).PlayToEnd(g);
            pool.Sort();

            json = WriteStars(json, t3, t2);
            File.WriteAllText(path, json, new UTF8Encoding(false));

            string line = string.Format(CultureInfo.InvariantCulture,
                "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9:0.00}\t{10:0.00}\t{11:0.00}",
                id, lv.PictureName, lv.Sequence.Count, g.State == GameState.Won ? g.MovesUsed : -1,
                pool.Count > 0 ? pool[0] : -1, pool.Count > 0 ? pool[pool.Count / 2] : -1,
                t3, t2, round, total == 0 ? 0 : (double)wins / total, f3, f2);
            report.AppendLine(line);
            Console.WriteLine(line);
        }
        File.WriteAllText("star_report_" + from + "_" + to + ".tsv", report.ToString(), new UTF8Encoding(false));
        return 0;
    }

    /// <summary>사람 흉내 봇으로 runs판을 두고, 깬 판의 수 목록을 돌려준다.</summary>
    static List<int> WinMoves(LevelData lv, int runs, int seedBlock, ref int wins, ref int total)
    {
        var list = new List<int>();
        for (int i = 0; i < runs; i++)
        {
            var game = new GameSession(lv);
            var bot = new AutoSolver(seedBlock * 1000003 + i * 7919, HumanNoise);
            total++;
            if (bot.PlayToEnd(game)) { wins++; list.Add(game.MovesUsed); }
        }
        return list;
    }

    static double Share(List<int> moves, int threshold)
    {
        if (moves.Count == 0) return 0;
        int n = 0;
        foreach (int m in moves) if (m <= threshold) n++;
        return (double)n / moves.Count;
    }

    /// <summary>목표 비율에 가장 가까운 정수 기준. 별 2개 기준은 별 3개 기준보다 커야 한다.</summary>
    static void Fit(List<int> moves, out int t3, out int t2)
    {
        if (moves.Count == 0) { t3 = 0; t2 = 0; return; }
        int lo = int.MaxValue, hi = int.MinValue;
        foreach (int m in moves) { lo = Math.Min(lo, m); hi = Math.Max(hi, m); }
        t3 = Closest(moves, lo, hi, Target3);
        t2 = Closest(moves, Math.Min(t3 + 1, hi + 1), hi + 1, Target2);
        if (t2 <= t3) t2 = t3 + 1;
    }

    static int Closest(List<int> moves, int lo, int hi, double target)
    {
        int best = lo;
        double bestErr = double.MaxValue;
        for (int t = lo; t <= hi; t++)
        {
            double err = Math.Abs(Share(moves, t) - target);
            if (err < bestErr - 1e-9) { bestErr = err; best = t; }
        }
        return best;
    }

    /// <summary>레벨 JSON의 다른 부분은 건드리지 않고 "stars" 줄만 넣거나 바꾼다.</summary>
    static string WriteStars(string json, int t3, int t2)
    {
        string line = "  \"stars\": {\"three\": " + t3 + ", \"two\": " + t2 + "},";
        var re = new Regex("^  \"stars\": \\{[^}]*\\},\\s*$", RegexOptions.Multiline);
        if (re.IsMatch(json)) return re.Replace(json, line);
        var brush = new Regex("^(  \"brush\": [0-9]+,)\\s*$", RegexOptions.Multiline);
        return brush.Replace(json, "$1\n" + line, 1);
    }
}
