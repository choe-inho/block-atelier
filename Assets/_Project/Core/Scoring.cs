using System;

namespace BlockAtelier.Core
{
    /// <summary>
    /// 한 판의 별과 시간 판정. 서버도 같은 규칙으로 검증한다.
    ///
    /// 별 (완성했을 때만):
    ///   ★★★  수 ≤ Star3Moves   (보통 실력으로 깬 판의 약 25%)
    ///   ★★   수 ≤ Star2Moves   (약 65%)
    ///   ★    그 밖, 또는 이어하기를 쓴 판
    ///   기준은 LevelTool이 자동 풀이 봇 시뮬레이션으로 레벨마다 보정한다.
    ///   시간이 아니라 수로 매기는 이유: 천천히 생각해도 손해가 없고, 같은 수면 누구에게나 같은 결과라 공정하다.
    ///
    /// 시간 (기록과 랭킹용):
    ///   첫 블록을 놓은 순간부터 완성까지, 실제로 조작할 수 있던 시간만 센다.
    ///   연출 중, 창이 열려 있을 때, 앱이 내려가 있을 때는 멈춘다. 이어하기를 쓴 판은 시간 기록에서 빠진다.
    /// </summary>
    public static class Scoring
    {
        public const int MaxStars = 3;

        public static int Stars(LevelData lv, int moves, int continuesUsed)
        {
            if (continuesUsed > 0) return 1;
            if (lv.Star3Moves <= 0) return 3;
            if (moves <= lv.Star3Moves) return 3;
            if (lv.Star2Moves > 0 && moves <= lv.Star2Moves) return 2;
            return 1;
        }

        /// <summary>지금 수로 끝나면 받을 별 (게이지 표시용). 이어하기를 썼으면 1.</summary>
        public static int StarsIfFinishedNow(LevelData lv, int moves, int continuesUsed)
        {
            return Stars(lv, moves, continuesUsed);
        }

        /// <summary>다음 별을 잃기까지 남은 수. 이미 별 1개면 -1.</summary>
        public static int MovesLeftForStars(LevelData lv, int moves, int continuesUsed, out int starsAtRisk)
        {
            starsAtRisk = Stars(lv, moves, continuesUsed);
            if (starsAtRisk == 3 && lv.Star3Moves > 0) return lv.Star3Moves - moves;
            if (starsAtRisk == 2) return lv.Star2Moves - moves;
            return -1;
        }

        public static bool TimeCounts(int continuesUsed) { return continuesUsed == 0; }

        /// <summary>0:42 / 1:05 / 12:30 (분:초). 한 시간을 넘으면 59:59로 고정.</summary>
        public static string FormatTime(long ms)
        {
            if (ms < 0) return "-";
            long s = Math.Min(ms / 1000, 59 * 60 + 59);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        /// <summary>랭킹처럼 세밀한 표시: 42.37초 / 1:05.2</summary>
        public static string FormatTimePrecise(long ms)
        {
            if (ms < 0) return "-";
            if (ms < 60000) return (ms / 1000) + "." + (ms % 1000 / 10).ToString("00") + "초";
            return FormatTime(ms) + "." + (ms % 1000 / 100);
        }
    }

    /// <summary>
    /// 한 판의 시간을 재는 시계. 엔진 없이 테스트할 수 있게 코어에 둔다.
    /// 화면 쪽은 매 프레임 Tick(dt, 지금 조작 가능한가)만 부르면 된다.
    /// </summary>
    public sealed class PlayClock
    {
        public long ElapsedMs { get; private set; }
        public bool Started { get; private set; }
        double carry;

        /// <summary>첫 블록을 놓을 때 부른다. 그 전의 망설임은 세지 않는다.</summary>
        public void Start() { Started = true; }

        public void Tick(double seconds, bool counting)
        {
            if (!Started || !counting || seconds <= 0) return;
            // 프레임 멈춤(앱 전환 복귀 등)으로 한 번에 큰 값이 들어오면 무시한다
            if (seconds > 0.5) seconds = 0.5;
            carry += seconds * 1000.0;
            long whole = (long)carry;
            ElapsedMs += whole;
            carry -= whole;
        }

        public void Reset() { ElapsedMs = 0; Started = false; carry = 0; }
    }
}
