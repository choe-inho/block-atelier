using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BlockAtelier.Core
{
    /// <summary>
    /// 의존성 없는 작은 JSON 파서/직렬화기.
    /// 객체는 Dictionary&lt;string, object&gt;, 배열은 List&lt;object&gt;, 숫자는 double.
    /// Unity JsonUtility는 중첩 배열을 못 다루고, 코어는 UnityEngine을 쓰지 않으므로 직접 둔다.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            var p = new Parser(json);
            p.SkipWs();
            object v = p.Value();
            p.SkipWs();
            if (!p.End) throw p.Error("JSON 끝에 남은 문자가 있음");
            return v;
        }

        sealed class Parser
        {
            readonly string s;
            int i;
            public Parser(string s) { this.s = s; }
            public bool End { get { return i >= s.Length; } }

            public FormatException Error(string msg) { return new FormatException(msg + " (위치 " + i + ")"); }

            public void SkipWs()
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }

            public object Value()
            {
                if (End) throw Error("값이 없음");
                char c = s[i];
                if (c == '{') return Obj();
                if (c == '[') return Arr();
                if (c == '"') return Str();
                if (c == 't') { Expect("true"); return true; }
                if (c == 'f') { Expect("false"); return false; }
                if (c == 'n') { Expect("null"); return null; }
                return Num();
            }

            void Expect(string word)
            {
                if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw Error(word + " 예상");
                i += word.Length;
            }

            Dictionary<string, object> Obj()
            {
                var d = new Dictionary<string, object>();
                i++;
                SkipWs();
                if (!End && s[i] == '}') { i++; return d; }
                while (true)
                {
                    SkipWs();
                    if (End || s[i] != '"') throw Error("키 문자열 예상");
                    string k = Str();
                    SkipWs();
                    if (End || s[i] != ':') throw Error(": 예상");
                    i++;
                    SkipWs();
                    d[k] = Value();
                    SkipWs();
                    if (End) throw Error("} 예상");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return d; }
                    throw Error(", 또는 } 예상");
                }
            }

            List<object> Arr()
            {
                var a = new List<object>();
                i++;
                SkipWs();
                if (!End && s[i] == ']') { i++; return a; }
                while (true)
                {
                    SkipWs();
                    a.Add(Value());
                    SkipWs();
                    if (End) throw Error("] 예상");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return a; }
                    throw Error(", 또는 ] 예상");
                }
            }

            string Str()
            {
                var sb = new StringBuilder();
                i++;
                while (true)
                {
                    if (End) throw Error("문자열이 닫히지 않음");
                    char c = s[i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (End) throw Error("잘못된 이스케이프");
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length) throw Error("잘못된 \\u");
                            sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            i += 4;
                            break;
                        default: throw Error("잘못된 이스케이프");
                    }
                }
            }

            double Num()
            {
                int start = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                if (start == i) throw Error("알 수 없는 값");
                return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }

        // ---------- 직렬화 ----------

        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value);
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            var str = v as string;
            if (str != null) { WriteString(sb, str); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is int || v is long || v is double || v is float)
            {
                sb.Append(Convert.ToDouble(v).ToString("R", CultureInfo.InvariantCulture));
                return;
            }
            var dict = v as IDictionary<string, object>;
            if (dict != null)
            {
                sb.Append('{');
                bool first = true;
                foreach (var kv in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, kv.Key);
                    sb.Append(':');
                    Write(sb, kv.Value);
                }
                sb.Append('}');
                return;
            }
            var list = v as System.Collections.IEnumerable;
            if (list != null)
            {
                sb.Append('[');
                bool first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Write(sb, item);
                }
                sb.Append(']');
                return;
            }
            WriteString(sb, v.ToString());
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
