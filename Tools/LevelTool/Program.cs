// 레벨 생성기: 그림(pictures.json)과 레벨 계획으로 블록 순서를 만들고,
// 자동 풀이 봇으로 클리어율을 재서 목표에 가장 가까운 순서를 고른다.
// Unity 밖에서 mono로 실행: mcs로 Core와 함께 컴파일 후 mono LevelTool.exe <pictures.json> <출력 폴더>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BlockAtelier.Core;
using BlockAtelier.Core.Sim;

static class LevelTool
{
    sealed class Plan
    {
        public int Id;
        public string Difficulty;
        public double Target;      // 목표 첫 시도 클리어율
        public string Tier;        // 블록 모양 묶음
        public bool Gray;
        public double MinFactor, MaxFactor;
        public double TargetDrops;
    }

    // 난이도 곡선 (기획서 8장): 1~5는 실패 불가, 이후 5레벨마다 쉬움-보통-보통-쉬움-어려움 리듬.
    // 뒤로 갈수록 목표 클리어율이 조금씩 내려가고, 21레벨부터 다 칠한 색은 회색이 된다.
    static Plan[] Plans = MakePlans();

    static Plan[] MakePlans()
    {
        var list = new List<Plan>();
        for (int id = 1; id <= 100; id++)
        {
            double t = (id - 1) / 99.0;   // 0 → 1
            var p = new Plan { Id = id, Gray = id > 20 };
            if (id <= 5)
            {
                p.Difficulty = "easy"; p.Target = 1.0; p.Tier = "easy"; p.MinFactor = 2.0; p.MaxFactor = 3.2;
            }
            else
            {
                int slot = (id - 1) % 5;   // 0 쉬움, 1 보통, 2 보통, 3 쉬움, 4 어려움
                if (slot == 4)
                {
                    p.Difficulty = "hard"; p.Target = 0.45 - 0.10 * t; p.Tier = "hard"; p.MinFactor = 0.7; p.MaxFactor = 3.0;
                }
                else if (slot == 0 || slot == 3)
                {
                    p.Difficulty = "easy"; p.Target = 0.95 - 0.10 * t; p.Tier = id <= 30 ? "easy" : "normal"; p.MinFactor = 1.4; p.MaxFactor = 4.2;
                }
                else
                {
                    p.Difficulty = "normal"; p.Target = 0.85 - 0.20 * t; p.Tier = "normal"; p.MinFactor = 1.0; p.MaxFactor = 3.6;
                }
            }
            p.Target = Math.Round(p.Target, 2);
            // 필요한 방울 수: 초반 45 → 후반 110. 붓 크기 = 픽셀 수 / 목표 방울 (올림)
            p.TargetDrops = 45 + 65 * t;
            list.Add(p);
        }
        return list.ToArray();
    }

    sealed class Candidate
    {
        public LevelData Level;
        public double Factor, Err;
    }

    // 평균 사람 실력을 흉내 내는 봇 흔들림. 실제 플레이 테스트 후 보정한다.
    const double HumanNoise = 25;

    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        string picturesPath = args.Length > 0 ? args[0] : "pictures.json";
        string outDir = args.Length > 1 ? args[1] : "Levels";
        int searchRuns = args.Length > 2 ? int.Parse(args[2]) : 40;
        int confirmRuns = args.Length > 3 ? int.Parse(args[3]) : 200;
        int from = args.Length > 4 ? int.Parse(args[4]) : 1;
        int to = args.Length > 5 ? int.Parse(args[5]) : Plans.Length;
        Directory.CreateDirectory(outDir);

        var pics = (List<object>)MiniJson.Parse(File.ReadAllText(picturesPath, Encoding.UTF8));
        var report = new StringBuilder();
        report.AppendLine("level\tpicture\tcolors\tpixels\tbrush\tblocks\tfactor\ttarget\tclearRate\tgreedyWin\tavgMoves");

        foreach (var plan in Plans)
        {
            if (plan.Id < from || plan.Id > to) continue;
            var pic = (Dictionary<string, object>)pics[plan.Id - 1];
            var candidates = new List<Candidate>();

            // 1단계 탐색: 공급 배율과 시드를 바꿔 가며 적은 횟수로 대략의 클리어율을 잰다.
            for (int step = 0; step <= 8; step++)
            {
                double factor = plan.MinFactor + (plan.MaxFactor - plan.MinFactor) * step / 8.0;
                for (int seed = 0; seed < 3; seed++)
                {
                    var lv = Build(plan, pic, factor, plan.Id * 1000 + step * 10 + seed);
                    if (!GreedyWins(lv)) continue;   // 최선으로 두면 반드시 깰 수 있어야 한다
                    double moves;
                    double rate = AutoSolver.MeasureClearRate(lv, searchRuns, HumanNoise, 17, out moves);
                    candidates.Add(new Candidate { Level = lv, Factor = factor, Err = Math.Abs(rate - plan.Target) });
                }
            }
            if (candidates.Count == 0) { Console.WriteLine("L" + plan.Id + ": 깰 수 있는 순서를 못 찾음"); return 1; }

            // 2단계 확인: 가까운 후보 6개를 많은 횟수로 다시 재서 진짜 가까운 것을 고른다.
            // 같은 오차면 공급이 적은(짧고 긴장감 있는) 쪽.
            candidates.Sort((a, b) => a.Err != b.Err ? a.Err.CompareTo(b.Err) : a.Factor.CompareTo(b.Factor));
            LevelData best = null;
            double bestErr = double.MaxValue, finalRate = 0, avgMoves = 0, bestFactor = 0;
            for (int i = 0; i < Math.Min(4, candidates.Count); i++)
            {
                var c = candidates[i];
                double moves;
                double rate = AutoSolver.MeasureClearRate(c.Level, confirmRuns, HumanNoise, 99991, out moves);
                double err = Math.Abs(rate - plan.Target);
                if (err < bestErr - 1e-9 || (Math.Abs(err - bestErr) < 1e-9 && c.Factor < bestFactor))
                {
                    best = c.Level; bestErr = err; finalRate = rate; avgMoves = moves; bestFactor = c.Factor;
                }
            }
            best.TargetClearRate = plan.Target;
            string path = Path.Combine(outDir, "level_" + plan.Id.ToString("000") + ".json");
            File.WriteAllText(path, Pretty(best), new UTF8Encoding(false));

            var gp = new GameSession(best).Picture;
            string line = string.Format(CultureInfo.InvariantCulture,
                "{0}\t{1}\t{2}\t{3}\t{10}\t{4}\t{5:0.00}\t{6:0.00}\t{7:0.00}\t{8}\t{9:0.0}",
                plan.Id, best.PictureName, best.ColorCount, gp.TotalPixels, best.Sequence.Count,
                bestFactor, plan.Target, finalRate, GreedyWins(best) ? "yes" : "no", avgMoves, best.Brush);
            report.AppendLine(line);
            Console.WriteLine(line);
        }

        File.WriteAllText(Path.Combine(outDir, "balance_report_" + from + "_" + to + ".tsv"), report.ToString(), new UTF8Encoding(false));
        return 0;
    }

    static bool GreedyWins(LevelData lv)
    {
        return new AutoSolver(1, 0).PlayToEnd(new GameSession(lv));
    }

    static LevelData Build(Plan plan, Dictionary<string, object> pic, double factor, int seed)
    {
        var lv = new LevelData
        {
            Id = plan.Id,
            Album = (string)pic["album"],
            AlbumTitle = (string)pic["albumTitle"],
            Difficulty = plan.Difficulty,
            TargetClearRate = plan.Target,
            GrayOnComplete = plan.Gray,
            PictureName = (string)pic["name"],
        };
        foreach (var c in (List<object>)pic["palette"]) lv.Palette.Add((string)c);
        foreach (var r in (List<object>)pic["rows"]) lv.Rows.Add((string)r);
        foreach (var r in (List<object>)pic["shades"]) lv.Shades.Add((string)r);
        lv.LineColor = (string)pic["lineColor"];
        lv.PictureHeight = lv.Rows.Count;
        lv.PictureWidth = lv.Rows[0].Length;
        if (plan.Tier == "hard") lv.Mechanics.Add("bigBlocks");

        int colors = lv.ColorCount;
        var need = new double[colors + 1];
        double totalNeed = 0;
        foreach (var r in lv.Rows)
            foreach (char ch in r)
                if (ch != '0' && ch != 'L') { need[ch - '0']++; totalNeed++; }
        lv.Brush = Math.Max(1, (int)Math.Ceiling(totalNeed / plan.TargetDrops));
        // 붓 크기만큼 방울이 덜 필요하다 (색마다 올림)
        totalNeed = 0;
        for (int c = 1; c <= colors; c++) { need[c] = Math.Ceiling(need[c] / lv.Brush); totalNeed += need[c]; }

        var rng = new Random(seed);
        var pool = ShapePool(plan.Tier);
        double supply = 0, target = totalNeed * factor;
        int prevColor = 0;
        // 색은 필요량에 비례해 뽑되, 같은 색이 이어질 확률을 높여 붓질(단색 줄)을 노릴 수 있게 한다.
        while (supply < target)
        {
            int color;
            if (prevColor != 0 && rng.NextDouble() < 0.45) color = prevColor;
            else color = WeightedColor(need, rng);
            string shape = WeightedShape(pool, rng);
            lv.Sequence.Add(new Block(shape, color));
            supply += Shape.Get(shape).Size;
            prevColor = color;
        }
        lv.Validate();
        return lv;
    }

    static int WeightedColor(double[] need, Random rng)
    {
        double sum = 0;
        // 필요량에 비례 (다 칠한 색은 컨베이어에서 필요한 색으로 바뀌므로 적은 색을 억지로 늘리지 않는다)
        for (int c = 1; c < need.Length; c++) sum += need[c];
        double t = rng.NextDouble() * sum;
        for (int c = 1; c < need.Length; c++)
        {
            t -= need[c];
            if (t <= 0) return c;
        }
        return need.Length - 1;
    }

    static List<KeyValuePair<string, double>> ShapePool(string tier)
    {
        var p = new List<KeyValuePair<string, double>>();
        Action<string, double> add = (s, w) => p.Add(new KeyValuePair<string, double>(s, w));
        bool hard = tier == "hard", normal = tier != "easy";
        add("O1", hard ? 0.3 : 1);
        add("I2H", hard ? 0.8 : 2); add("I2V", hard ? 0.8 : 2);
        add("I3H", 3); add("I3V", 3);
        add("I4H", 3); add("I4V", 3);
        add("O4", 3);
        foreach (var s in new[] { "L3A", "L3B", "L3C", "L3D" }) add(s, 1);
        if (normal)
        {
            add("I5H", 1.5); add("I5V", 1.5);
            add("R6H", 0.8); add("R6V", 0.8);
            foreach (var s in new[] { "T4A", "T4B", "T4C", "T4D" }) add(s, 0.7);
            foreach (var s in new[] { "L4A", "L4B", "L4C", "L4D", "J4A", "J4B", "J4C", "J4D" }) add(s, 0.5);
            foreach (var s in new[] { "S4H", "S4V", "Z4H", "Z4V" }) add(s, 0.5);
        }
        if (hard)
        {
            add("O9", 1.2);
            foreach (var s in new[] { "L5A", "L5B", "L5C", "L5D" }) add(s, 0.7);
        }
        return p;
    }

    static string WeightedShape(List<KeyValuePair<string, double>> pool, Random rng)
    {
        double sum = 0;
        foreach (var kv in pool) sum += kv.Value;
        double t = rng.NextDouble() * sum;
        foreach (var kv in pool) { t -= kv.Value; if (t <= 0) return kv.Key; }
        return pool[pool.Count - 1].Key;
    }

    /// <summary>사람이 읽고 고치기 쉬운 JSON. 블록 한 개당 한 줄.</summary>
    static string Pretty(LevelData lv)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.AppendFormat(CultureInfo.InvariantCulture, "  \"id\": {0},\n", lv.Id);
        sb.AppendFormat("  \"album\": \"{0}\",\n", lv.Album);
        sb.AppendFormat("  \"albumTitle\": \"{0}\",\n", lv.AlbumTitle);
        sb.AppendFormat(CultureInfo.InvariantCulture, "  \"brush\": {0},\n", lv.Brush);
        if (lv.Star3Moves > 0)   // 별 기준은 StarTool이 따로 넣는다 (레벨을 다시 만들면 StarTool도 다시 실행)
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"stars\": {{\"three\": {0}, \"two\": {1}}},\n", lv.Star3Moves, lv.Star2Moves);
        sb.AppendFormat("  \"difficulty\": \"{0}\",\n", lv.Difficulty);
        sb.AppendFormat(CultureInfo.InvariantCulture, "  \"targetClearRate\": {0},\n", lv.TargetClearRate);
        sb.AppendFormat("  \"grayOnComplete\": {0},\n", lv.GrayOnComplete ? "true" : "false");
        sb.Append("  \"mechanics\": [" + string.Join(", ", lv.Mechanics.ConvertAll(m => "\"" + m + "\"")) + "],\n");
        sb.Append("  \"picture\": {\n");
        sb.AppendFormat("    \"name\": \"{0}\",\n", lv.PictureName);
        sb.AppendFormat("    \"w\": {0}, \"h\": {1},\n", lv.PictureWidth, lv.PictureHeight);
        sb.Append("    \"palette\": [" + string.Join(", ", lv.Palette.ConvertAll(c => "\"" + c + "\"")) + "],\n");
        sb.Append("    \"rows\": [\n");
        for (int i = 0; i < lv.Rows.Count; i++)
            sb.Append("      \"" + lv.Rows[i] + "\"" + (i < lv.Rows.Count - 1 ? "," : "") + "\n");
        sb.Append("    ],\n");
        sb.Append("    \"shades\": [\n");
        for (int i = 0; i < lv.Shades.Count; i++)
            sb.Append("      \"" + lv.Shades[i] + "\"" + (i < lv.Shades.Count - 1 ? "," : "") + "\n");
        sb.Append("    ],\n");
        sb.AppendFormat("    \"lineColor\": \"{0}\"\n", lv.LineColor);
        sb.Append("  },\n");
        sb.Append("  \"sequence\": [\n");
        for (int i = 0; i < lv.Sequence.Count; i++)
            sb.AppendFormat("    {{\"shape\": \"{0}\", \"color\": {1}}}{2}\n",
                lv.Sequence[i].ShapeCode, lv.Sequence[i].Color, i < lv.Sequence.Count - 1 ? "," : "");
        sb.Append("  ]\n}\n");
        return sb.ToString();
    }
}
