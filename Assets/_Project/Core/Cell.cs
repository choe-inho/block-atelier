namespace BlockAtelier.Core
{
    /// <summary>
    /// 보드 칸과 블록 색에 쓰는 값.
    /// 1~9는 레벨 팔레트 인덱스(그림 색), 0은 빈칸.
    /// </summary>
    public static class Cell
    {
        public const int Empty = 0;

        /// <summary>그림에 더 이상 필요 없는 색. 줄은 채우지만 페인트는 없다.</summary>
        public const int Gray = -1;

        /// <summary>무지개 세탁 부스터로 만든 만능 칸. 지워지면 가장 많이 남은 색을 칠한다.</summary>
        public const int Wild = 99;

        public static bool IsColor(int v) { return v > 0 && v != Wild; }
        public static bool IsFilled(int v) { return v != Empty; }
    }
}
