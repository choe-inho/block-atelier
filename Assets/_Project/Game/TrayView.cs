using System.Collections.Generic;
using BlockAtelier.Core;
using UnityEngine;

namespace BlockAtelier.Game
{
    /// <summary>
    /// 아래쪽 보관함 1칸 + 컨베이어 5칸 (앞 3칸 선택, 뒤 2칸 미리 보기).
    /// </summary>
    public sealed class TrayView
    {
        public readonly Transform Root;
        public const float SlotH = 1.9f;

        struct Slot
        {
            public Vector2 Center;
            public Vector2 Size;
            public SpriteRenderer Bg;
            public Transform Piece;
            public bool Selectable;
        }

        readonly Slot[] slots = new Slot[6];   // 0 = 보관, 1~5 = 컨베이어
        readonly BoardView board;
        public UnityEngine.UI.Text HoldLabel;

        public TrayView(Transform parent, Canvas canvas, BoardView boardView)
        {
            board = boardView;
            Root = new GameObject("Tray").transform;
            Root.SetParent(parent, false);

            Make(0, new Vector2(-4.05f, 0f), new Vector2(1.5f, SlotH), true, true);
            float[] xs = { -2.075f, -0.075f, 1.925f };
            for (int i = 0; i < 3; i++) Make(i + 1, new Vector2(xs[i], 0f), new Vector2(1.85f, SlotH), true, false);
            Make(4, new Vector2(3.42f, 0f), new Vector2(0.9f, 1.3f), false, false);
            Make(5, new Vector2(4.4f, 0f), new Vector2(0.9f, 1.3f), false, false);

            HoldLabel = UI.Label(canvas, "보관", new Vector2(-4.05f, SlotH * 0.5f - 0.22f), 0.22f, UI.Muted);
        }

        void Make(int i, Vector2 c, Vector2 size, bool selectable, bool hold)
        {
            var col = hold ? Gfx.Hex("#252A40") : (selectable ? UI.Button : Gfx.Hex("#1D2133"));
            var bg = UI.Panel(hold ? "Hold" : "Slot", Root, c, size, col, 10);
            slots[i] = new Slot { Center = c, Size = size, Bg = bg, Selectable = selectable };
        }

        public static int SlotIndex(BlockSource src) { return src.FromHold ? 0 : src.Index + 1; }

        public Vector3 SlotWorld(BlockSource src) { return Root.TransformPoint(slots[SlotIndex(src)].Center); }

        public bool HitHold(Vector3 world) { return Hit(0, world, 0.25f); }

        bool Hit(int i, Vector3 world, float margin)
        {
            var s = slots[i];
            var l = Root.InverseTransformPoint(world);
            return Mathf.Abs(l.x - s.Center.x) <= s.Size.x * 0.5f + margin && Mathf.Abs(l.y - s.Center.y) <= s.Size.y * 0.5f + margin;
        }

        /// <summary>누른 곳의 블록 출처. 없으면 false.</summary>
        public bool HitSource(Vector3 world, GameSession game, out BlockSource src)
        {
            src = default(BlockSource);
            for (int i = 1; i <= 3; i++)
            {
                if (!Hit(i, world, 0.08f)) continue;
                if (i - 1 >= game.Conveyor.SelectableSlots) return false;
                src = BlockSource.Conveyor(i - 1);
                return true;
            }
            if (Hit(0, world, 0.08f) && !game.Conveyor.Hold(0).IsNone)
            {
                src = BlockSource.Held(0);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 다시 그린다. shiftFrom ≥ 0이면 그 칸부터 뒤쪽 블록이 오른쪽에서 밀려 들어오는 연출.
        /// </summary>
        public void Refresh(GameSession game, int shiftFrom = -1, BlockSource? hidden = null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Piece != null) Object.Destroy(slots[i].Piece.gameObject);
                slots[i].Piece = null;
            }

            Build(0, game.Conveyor.Hold(0), game, false);
            for (int k = 0; k < ConveyorModel.VisibleCount; k++)
            {
                var b = game.Conveyor.Visible(k);
                Build(k + 1, b, game, k < 3);
                if (shiftFrom >= 0 && k >= shiftFrom && slots[k + 1].Piece != null)
                {
                    var t = slots[k + 1].Piece;
                    var to = t.localPosition;
                    var from = k + 2 < slots.Length ? (Vector3)slots[k + 2].Center : to + new Vector3(1.2f, 0f, 0f);
                    float delay = (k - shiftFrom) * 0.03f;
                    t.localPosition = from;
                    Tween.Run(0.22f, x => t.localPosition = Vector3.LerpUnclamped(from, to, Ease.OutBack(x)), null, delay);
                }
            }
            if (hidden.HasValue) SetHidden(hidden.Value, true);
        }

        void Build(int i, Block b, GameSession game, bool selectableSlot)
        {
            if (b.IsNone) return;
            var s = slots[i];
            var shape = b.Shape;
            float maxW = s.Size.x - 0.35f, maxH = s.Size.y - (i == 0 ? 0.55f : 0.35f);
            float pitch = Mathf.Min(0.4f, maxW / shape.Width, maxH / shape.Height);
            if (!s.Selectable) pitch = Mathf.Min(pitch, 0.19f);

            var root = new GameObject("Piece").transform;
            root.SetParent(Root, false);
            root.localPosition = new Vector3(s.Center.x, s.Center.y - (i == 0 ? 0.12f : 0f), 0f);
            var color = board.ColorOf(b.Color);
            bool dead = (i <= 3) && !game.Board.CanPlaceAnywhere(shape);
            if (dead) color = Color.Lerp(color, UI.Well, 0.65f);
            for (int k = 0; k < shape.Size; k++)
            {
                var sr = Gfx.MakeSprite("c", root, Gfx.Block, color, 11);
                sr.transform.localPosition = new Vector3((shape.Xs[k] - (shape.Width - 1) * 0.5f) * pitch,
                                                         ((shape.Height - 1) * 0.5f - shape.Ys[k]) * pitch, 0f);
                sr.transform.localScale = Vector3.one * pitch * 0.93f;
            }
            slots[i].Piece = root;
        }

        public void SetHidden(BlockSource src, bool hidden)
        {
            var p = slots[SlotIndex(src)].Piece;
            if (p != null) p.gameObject.SetActive(!hidden);
        }

        public void HighlightHold(bool on)
        {
            var bg = slots[0].Bg;
            bg.color = on ? Gfx.Hex("#5A4E3A") : Gfx.Hex("#252A40");
        }

        /// <summary>블록을 집을 때 슬롯이 살짝 눌리는 연출</summary>
        public void Nudge(BlockSource src)
        {
            var bg = slots[SlotIndex(src)].Bg.transform;
            Tween.Run(0.18f, x => bg.localScale = Vector3.one * (1f - 0.06f * Ease.Bump(x)));
        }
    }
}
