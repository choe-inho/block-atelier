using System.Collections.Generic;
using BlockAtelier.Core;
using UnityEngine;

namespace BlockAtelier.Game
{
    /// <summary>8x8 보드 그리기. 규칙 판단은 하지 않고 GameSession 상태를 보여 주기만 한다.</summary>
    public sealed class BoardView
    {
        public const float Pitch = 1.08f;
        public const float BlockSize = 1.0f;
        const int N = BoardModel.Size;

        public readonly Transform Root;
        readonly SpriteRenderer[] blocks = new SpriteRenderer[N * N];
        readonly SpriteRenderer[] glows = new SpriteRenderer[N * N];
        readonly bool[] will = new bool[N * N];
        Color[] palette;
        public Color WillColor = Color.white;

        public BoardView(Transform parent)
        {
            Root = new GameObject("Board").transform;
            Root.SetParent(parent, false);
            float span = N * Pitch;
            UI.Panel("BoardPanel", Root, Vector2.zero, new Vector2(span + 0.36f, span + 0.36f), UI.BoardPanel, 1);
            for (int i = 0; i < N * N; i++)
            {
                var p = CellLocal(i);
                var well = Gfx.MakeSprite("Well", Root, Gfx.Well, UI.Well, 2);
                well.transform.localPosition = p;
                well.transform.localScale = Vector3.one * 0.96f;

                var b = Gfx.MakeSprite("Block", Root, Gfx.Block, Color.white, 4);
                b.transform.localPosition = p;
                b.transform.localScale = Vector3.one * BlockSize;
                b.enabled = false;
                blocks[i] = b;

                var g = Gfx.MakeSprite("Glow", Root, Gfx.Well, Color.white, 5);
                g.transform.localPosition = p;
                g.transform.localScale = Vector3.one * 0.98f;
                g.enabled = false;
                glows[i] = g;
            }
        }

        public void SetPalette(Color[] colors) { palette = colors; }

        public static Vector3 CellLocal(int index)
        {
            int x = index % N, y = index / N;
            return new Vector3((x - 3.5f) * Pitch, (3.5f - y) * Pitch, 0f);
        }

        public Vector3 CellWorld(int index) { return Root.TransformPoint(CellLocal(index)); }

        /// <summary>블록 묶음의 왼쪽 위 칸 월드 좌표로 보드 칸 좌표를 구한다.</summary>
        public Vector2Int GridFromWorld(Vector3 topLeftCellWorld)
        {
            var l = Root.InverseTransformPoint(topLeftCellWorld);
            int gx = Mathf.RoundToInt(l.x / Pitch + 3.5f);
            int gy = Mathf.RoundToInt(3.5f - l.y / Pitch);
            return new Vector2Int(gx, gy);
        }

        public Color ColorOf(int v)
        {
            if (v == Cell.Gray) return UI.GrayBlock;
            if (v == Cell.Wild) return Color.HSVToRGB(Mathf.Repeat(Time.time * 0.5f, 1f), 0.55f, 1f);
            if (palette != null && v > 0 && v < palette.Length) return palette[v];
            return Color.white;
        }

        public SpriteRenderer BlockAt(int i) { return blocks[i]; }

        /// <summary>모델 그대로 그린다. overrides에 있는 칸은 그 값으로 (지우기 직전 모습 보여 줄 때).</summary>
        public void Render(BoardModel board, Dictionary<int, int> overrides = null)
        {
            for (int i = 0; i < N * N; i++)
            {
                int v = board.Get(i);
                int o;
                if (overrides != null && overrides.TryGetValue(i, out o)) v = o;
                var b = blocks[i];
                b.transform.localScale = Vector3.one * BlockSize;
                b.transform.localRotation = Quaternion.identity;
                if (v == Cell.Empty) { b.enabled = false; continue; }
                b.enabled = true;
                b.color = ColorOf(v);
                b.sortingOrder = 4;
            }
        }

        public void ShowGhost(BoardModel board, Shape shape, int color, int gx, int gy)
        {
            ClearGhost(board);
            var tmp = board.Clone();
            var placed = tmp.Place(shape, color, gx, gy);
            var c = ColorOf(color);
            foreach (int i in placed)
            {
                var b = blocks[i];
                b.enabled = true;
                var gc = Color.Lerp(c, Color.white, 0.2f); gc.a = 0.5f;
                b.color = gc;
            }
            for (int y = 0; y < N; y++)
                if (tmp.IsRowFull(y)) for (int x = 0; x < N; x++) will[y * N + x] = true;
            for (int x = 0; x < N; x++)
                if (tmp.IsColFull(x)) for (int y = 0; y < N; y++) will[y * N + x] = true;
            WillColor = c;
            for (int i = 0; i < N * N; i++) glows[i].enabled = will[i];
        }

        public void ClearGhost(BoardModel board)
        {
            for (int i = 0; i < N * N; i++)
            {
                will[i] = false;
                glows[i].enabled = false;
            }
            Render(board);
        }

        /// <summary>매 프레임: 지워질 줄 반짝임, 만능 칸 무지개.</summary>
        public void Tick(BoardModel board)
        {
            float pulse = 0.28f + 0.22f * Mathf.Sin(Time.time * 10f);
            for (int i = 0; i < N * N; i++)
            {
                if (will[i])
                {
                    var c = Color.Lerp(WillColor, Color.white, 0.6f); c.a = pulse;
                    glows[i].color = c;
                }
                if (board.Get(i) == Cell.Wild && blocks[i].enabled) blocks[i].color = ColorOf(Cell.Wild);
            }
        }

        public void Desaturate(float amount)
        {
            for (int i = 0; i < N * N; i++)
            {
                if (!blocks[i].enabled) continue;
                var c = blocks[i].color;
                float g = c.grayscale;
                blocks[i].color = Color.Lerp(c, new Color(g, g, g, c.a) * 0.7f, amount);
            }
        }
    }
}
