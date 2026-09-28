using System;
using System.Collections.Generic;

namespace BlockAtelier.Core
{
    /// <summary>블록 모양. 회전은 없고, 방향마다 별도 코드를 쓴다 (블록 블라스트 방식).</summary>
    public sealed class Shape
    {
        public readonly string Code;
        public readonly int[] Xs;
        public readonly int[] Ys;
        public readonly int Width;
        public readonly int Height;

        public int Size { get { return Xs.Length; } }

        Shape(string code, int[] cells)
        {
            Code = code;
            int n = cells.Length / 2;
            Xs = new int[n];
            Ys = new int[n];
            int w = 0, h = 0;
            for (int i = 0; i < n; i++)
            {
                Xs[i] = cells[i * 2];
                Ys[i] = cells[i * 2 + 1];
                w = Math.Max(w, Xs[i] + 1);
                h = Math.Max(h, Ys[i] + 1);
            }
            Width = w;
            Height = h;
        }

        static readonly Dictionary<string, Shape> registry = new Dictionary<string, Shape>();

        public static IEnumerable<Shape> All { get { return registry.Values; } }

        public static Shape Get(string code)
        {
            Shape s;
            if (!registry.TryGetValue(code, out s))
                throw new ArgumentException("알 수 없는 블록 모양: " + code);
            return s;
        }

        public static bool Exists(string code) { return registry.ContainsKey(code); }

        static void Add(string code, params int[] cells) { registry[code] = new Shape(code, cells); }

        // 좌표는 (x, y) 쌍, y는 아래로 증가.
        static Shape()
        {
            Add("O1", 0, 0);
            Add("I2H", 0, 0, 1, 0);
            Add("I2V", 0, 0, 0, 1);
            Add("I3H", 0, 0, 1, 0, 2, 0);
            Add("I3V", 0, 0, 0, 1, 0, 2);
            Add("I4H", 0, 0, 1, 0, 2, 0, 3, 0);
            Add("I4V", 0, 0, 0, 1, 0, 2, 0, 3);
            Add("I5H", 0, 0, 1, 0, 2, 0, 3, 0, 4, 0);
            Add("I5V", 0, 0, 0, 1, 0, 2, 0, 3, 0, 4);
            Add("O4", 0, 0, 1, 0, 0, 1, 1, 1);
            Add("O9", 0, 0, 1, 0, 2, 0, 0, 1, 1, 1, 2, 1, 0, 2, 1, 2, 2, 2);
            Add("R6H", 0, 0, 1, 0, 2, 0, 0, 1, 1, 1, 2, 1);
            Add("R6V", 0, 0, 1, 0, 0, 1, 1, 1, 0, 2, 1, 2);
            // 3칸 꺾임 (2x2에서 한 모서리 빠짐)
            Add("L3A", 0, 0, 0, 1, 1, 1);
            Add("L3B", 1, 0, 0, 1, 1, 1);
            Add("L3C", 0, 0, 1, 0, 0, 1);
            Add("L3D", 0, 0, 1, 0, 1, 1);
            // L, J, T, S, Z (4칸)
            Add("L4A", 0, 0, 0, 1, 0, 2, 1, 2);
            Add("L4B", 0, 0, 1, 0, 2, 0, 0, 1);
            Add("L4C", 0, 0, 1, 0, 1, 1, 1, 2);
            Add("L4D", 2, 0, 0, 1, 1, 1, 2, 1);
            Add("J4A", 1, 0, 1, 1, 1, 2, 0, 2);
            Add("J4B", 0, 0, 0, 1, 1, 1, 2, 1);
            Add("J4C", 0, 0, 1, 0, 0, 1, 0, 2);
            Add("J4D", 0, 0, 1, 0, 2, 0, 2, 1);
            Add("T4A", 0, 0, 1, 0, 2, 0, 1, 1);
            Add("T4B", 1, 0, 0, 1, 1, 1, 2, 1);
            Add("T4C", 0, 0, 0, 1, 0, 2, 1, 1);
            Add("T4D", 1, 0, 1, 1, 1, 2, 0, 1);
            Add("S4H", 1, 0, 2, 0, 0, 1, 1, 1);
            Add("S4V", 0, 0, 0, 1, 1, 1, 1, 2);
            Add("Z4H", 0, 0, 1, 0, 1, 1, 2, 1);
            Add("Z4V", 1, 0, 0, 1, 1, 1, 0, 2);
            // 5칸 큰 꺾임
            Add("L5A", 0, 0, 0, 1, 0, 2, 1, 2, 2, 2);
            Add("L5B", 0, 0, 1, 0, 2, 0, 0, 1, 0, 2);
            Add("L5C", 0, 0, 1, 0, 2, 0, 2, 1, 2, 2);
            Add("L5D", 2, 0, 2, 1, 0, 2, 1, 2, 2, 2);
        }
    }
}
