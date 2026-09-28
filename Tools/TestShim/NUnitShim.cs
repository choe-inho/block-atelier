// Unity 없이 mono로 테스트를 돌리기 위한 최소 NUnit 대용품.
// Unity 프로젝트에는 넣지 않는다 (Unity는 진짜 NUnit을 쓴다).
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Class)] public class TestFixtureAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class TestAttribute : Attribute { }

    public class AssertionException : Exception { public AssertionException(string m) : base(m) { } }

    public static class Assert
    {
        public static void AreEqual(object expected, object actual, string msg = null)
        {
            bool eq = expected is IConvertible && actual is IConvertible && !(expected is string)
                ? Convert.ToDouble(expected) == Convert.ToDouble(actual)
                : Equals(expected, actual);
            if (!eq) throw new AssertionException("기대 " + expected + ", 실제 " + actual + (msg == null ? "" : " — " + msg));
        }
        public static void IsTrue(bool c, string msg = null) { if (!c) throw new AssertionException("참이어야 함 " + msg); }
        public static void IsFalse(bool c, string msg = null) { if (c) throw new AssertionException("거짓이어야 함 " + msg); }
        public static void IsNotNull(object o, string msg = null) { if (o == null) throw new AssertionException("null이면 안 됨 " + msg); }
    }

    public static class CollectionAssert
    {
        public static void Contains(IEnumerable c, object item)
        {
            if (!c.Cast<object>().Any(x => Equals(x, item))) throw new AssertionException("컬렉션에 " + item + " 없음");
        }
    }
}

public static class TestRunner
{
    public static int Main()
    {
        int pass = 0, fail = 0;
        foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (t.GetCustomAttribute(typeof(NUnit.Framework.TestFixtureAttribute)) == null) continue;
            foreach (var m in t.GetMethods())
            {
                if (m.GetCustomAttribute(typeof(NUnit.Framework.TestAttribute)) == null) continue;
                try
                {
                    m.Invoke(Activator.CreateInstance(t), null);
                    pass++;
                    Console.WriteLine("  통과  " + m.Name);
                }
                catch (TargetInvocationException e)
                {
                    fail++;
                    Console.WriteLine("  실패  " + m.Name + ": " + e.InnerException.Message);
                }
            }
        }
        Console.WriteLine("결과: 통과 " + pass + ", 실패 " + fail);
        return fail == 0 ? 0 : 1;
    }
}
