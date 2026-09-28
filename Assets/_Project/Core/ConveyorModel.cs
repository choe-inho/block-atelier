using System.Collections.Generic;

namespace BlockAtelier.Core
{
    /// <summary>
    /// 컨베이어와 보관함.
    /// 보이는 칸 5개 중 앞 3개(0~2)만 고를 수 있고, 뒤 2개는 미리 보기.
    /// 앞쪽 블록을 쓰면 뒤가 한 칸씩 당겨지고 레벨 순서의 다음 블록이 끝에 들어온다.
    /// </summary>
    public sealed class ConveyorModel
    {
        public const int VisibleCount = 5;
        public const int SelectableCount = 3;

        readonly List<Block> sequence;   // 레벨의 블록 순서 (불변, 공유)
        int nextIndex;
        readonly List<Block> visible;
        Block hold = Block.None;
        int holdCapacity = 1;
        Block extraHold = Block.None;    // 보관함 +1 부스터용 두 번째 칸

        /// <summary>새 블록이 들어올 때 색을 바꾸는 규칙 (다 칠한 색 처리). GameSession이 정한다.</summary>
        public System.Func<int, int> ColorFilter;

        public ConveyorModel(List<Block> levelSequence)
        {
            sequence = levelSequence;
            visible = new List<Block>(VisibleCount);
            Refill();
        }

        ConveyorModel(ConveyorModel o)
        {
            sequence = o.sequence;
            nextIndex = o.nextIndex;
            visible = new List<Block>(o.visible);
            hold = o.hold;
            holdCapacity = o.holdCapacity;
            extraHold = o.extraHold;
            ColorFilter = o.ColorFilter;
        }

        public ConveyorModel Clone() { return new ConveyorModel(this); }

        public int VisibleSlots { get { return visible.Count; } }
        public int SelectableSlots { get { return visible.Count < SelectableCount ? visible.Count : SelectableCount; } }
        public Block Visible(int i) { return i < visible.Count ? visible[i] : Block.None; }
        public int RemainingInSequence { get { return sequence.Count - nextIndex; } }

        public int HoldCapacity { get { return holdCapacity; } }
        public Block Hold(int slot) { return slot == 0 ? hold : extraHold; }

        public bool IsEmpty
        {
            get { return visible.Count == 0 && hold.IsNone && extraHold.IsNone; }
        }

        public void SetHoldCapacity(int capacity) { holdCapacity = capacity < 1 ? 1 : (capacity > 2 ? 2 : capacity); }

        void Refill()
        {
            while (visible.Count < VisibleCount && nextIndex < sequence.Count)
            {
                var b = sequence[nextIndex++];
                if (ColorFilter != null) b = b.WithColor(ColorFilter(b.Color));
                visible.Add(b);
            }
        }

        public Block Get(BlockSource src)
        {
            if (src.FromHold) return Hold(src.Index);
            if (src.Index < 0 || src.Index >= SelectableSlots) return Block.None;
            return visible[src.Index];
        }

        /// <summary>블록을 꺼낸다 (보드에 놓을 때). 컨베이어에서 꺼내면 한 칸 전진.</summary>
        public Block Take(BlockSource src)
        {
            Block b = Get(src);
            if (b.IsNone) return b;
            if (src.FromHold)
            {
                if (src.Index == 0) hold = Block.None; else extraHold = Block.None;
            }
            else
            {
                visible.RemoveAt(src.Index);
                Refill();
            }
            return b;
        }

        /// <summary>
        /// 앞쪽 블록을 보관함에 넣는다. 빈 보관 칸이 있으면 넣고 컨베이어가 전진,
        /// 가득 차 있으면 0번 보관 칸과 맞바꾼다.
        /// </summary>
        public bool Stash(int conveyorIndex)
        {
            if (conveyorIndex < 0 || conveyorIndex >= SelectableSlots) return false;
            Block b = visible[conveyorIndex];
            if (hold.IsNone) { hold = b; visible.RemoveAt(conveyorIndex); Refill(); return true; }
            if (holdCapacity > 1 && extraHold.IsNone) { extraHold = b; visible.RemoveAt(conveyorIndex); Refill(); return true; }
            visible[conveyorIndex] = hold;
            hold = b;
            return true;
        }

        /// <summary>보관 칸이 비어 있고 컨베이어가 전진할 수 있어 새 블록을 볼 수 있는지.</summary>
        public bool CanRevealByStash
        {
            get
            {
                bool freeHold = hold.IsNone || (holdCapacity > 1 && extraHold.IsNone);
                return freeHold && SelectableSlots > 0 && RemainingInSequence > 0;
            }
        }

        /// <summary>조건에 맞는 모든 블록(컨베이어, 보관함)의 색을 바꾼다. 다 칠한 색을 회색으로 만들 때 사용.</summary>
        public void Recolor(int from, int to)
        {
            for (int i = 0; i < visible.Count; i++)
                if (visible[i].Color == from) visible[i] = visible[i].WithColor(to);
            if (!hold.IsNone && hold.Color == from) hold = hold.WithColor(to);
            if (!extraHold.IsNone && extraHold.Color == from) extraHold = extraHold.WithColor(to);
        }

        /// <summary>이어하기: 앞쪽 블록들을 주어진 블록으로 바꾼다.</summary>
        public void ReplaceFront(IList<Block> blocks)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (i < visible.Count) visible[i] = blocks[i];
                else visible.Add(blocks[i]);
            }
        }

        public IEnumerable<BlockSource> AllSources()
        {
            for (int i = 0; i < SelectableSlots; i++) yield return BlockSource.Conveyor(i);
            if (!hold.IsNone) yield return BlockSource.Held(0);
            if (!extraHold.IsNone) yield return BlockSource.Held(1);
        }
    }

    /// <summary>놓을 블록이 어디서 오는지: 컨베이어 앞쪽 칸 또는 보관함.</summary>
    public struct BlockSource
    {
        public readonly bool FromHold;
        public readonly int Index;

        BlockSource(bool fromHold, int index) { FromHold = fromHold; Index = index; }

        public static BlockSource Conveyor(int index) { return new BlockSource(false, index); }
        public static BlockSource Held(int slot) { return new BlockSource(true, slot); }

        public override string ToString() { return (FromHold ? "보관" : "컨베이어") + Index; }
    }
}
