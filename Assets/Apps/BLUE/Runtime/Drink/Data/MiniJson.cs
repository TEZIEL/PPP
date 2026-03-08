using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PPP.BLUE.VN.DrinkSystem
{
    // Lightweight JSON parser for runtime data loading.
    internal static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            return Parser.Parse(json);
        }

        private sealed class Parser : IDisposable
        {
            private const string WORD_BREAK = "{}[],:\"";
            private readonly string json;
            private int index;

            private Parser(string jsonString)
            {
                json = jsonString;
            }

            public static object Parse(string jsonString)
            {
                using (var parser = new Parser(jsonString))
                {
                    return parser.ParseValue();
                }
            }

            public void Dispose() { }

            private Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                index++;

                while (true)
                {
                    EatWhitespace();
                    if (index >= json.Length)
                        return table;

                    var c = json[index];
                    if (c == '}')
                    {
                        index++;
                        return table;
                    }

                    var key = ParseString();
                    EatWhitespace();
                    if (index < json.Length && json[index] == ':')
                        index++;

                    var value = ParseValue();
                    table[key] = value;

                    EatWhitespace();
                    if (index < json.Length && json[index] == ',')
                    {
                        index++;
                        continue;
                    }
                }
            }

            private List<object> ParseArray()
            {
                var array = new List<object>();
                index++;

                while (true)
                {
                    EatWhitespace();
                    if (index >= json.Length)
                        return array;

                    if (json[index] == ']')
                    {
                        index++;
                        return array;
                    }

                    array.Add(ParseValue());
                    EatWhitespace();
                    if (index < json.Length && json[index] == ',')
                        index++;
                }
            }

            private object ParseValue()
            {
                EatWhitespace();
                if (index >= json.Length)
                    return null;

                switch (json[index])
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': return ParseLiteral("true", true);
                    case 'f': return ParseLiteral("false", false);
                    case 'n': return ParseLiteral("null", null);
                    default: return ParseNumber();
                }
            }

            private object ParseLiteral(string literal, object value)
            {
                if (index + literal.Length <= json.Length &&
                    string.Compare(json, index, literal, 0, literal.Length, StringComparison.Ordinal) == 0)
                {
                    index += literal.Length;
                    return value;
                }

                return null;
            }

            private string ParseString()
            {
                var sb = new StringBuilder();
                index++;

                while (index < json.Length)
                {
                    char c = json[index++];
                    if (c == '"')
                        break;

                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }

                    if (index >= json.Length)
                        break;

                    c = json[index++];
                    switch (c)
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
                            if (index + 4 <= json.Length)
                            {
                                var hex = json.Substring(index, 4);
                                if (ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                                    sb.Append((char)code);
                                index += 4;
                            }
                            break;
                    }
                }

                return sb.ToString();
            }

            private object ParseNumber()
            {
                int lastIndex = GetLastIndexOfNumber(index);
                if (lastIndex < index)
                    return 0;

                var number = json.Substring(index, lastIndex - index + 1);
                index = lastIndex + 1;

                if (number.IndexOf('.') >= 0 || number.IndexOf('e') >= 0 || number.IndexOf('E') >= 0)
                {
                    if (double.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                        return d;
                    return 0d;
                }

                if (long.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out long l))
                    return l;

                return 0L;
            }

            private int GetLastIndexOfNumber(int idx)
            {
                while (idx < json.Length && "0123456789+-.eE".IndexOf(json[idx]) != -1)
                    idx++;
                return idx - 1;
            }

            private void EatWhitespace()
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                    index++;
            }
        }
    }
}
