using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CivilizationToSpace.Core.Json
{
    /// <summary>
    /// 外部依存を持たない最小のJSONリーダー。RFC 8259 に従う。
    /// 末尾カンマ、コメント、単一引用符、NaN・Infinity を受け付けない。
    /// 重複キーは後勝ちとし、ブラウザの JSON.parse と同じ結果にする。
    /// </summary>
    public static class JsonParser
    {
        /// <summary>入れ子の上限。壊れた入力による深い再帰を防ぐ。</summary>
        private const int MaxDepth = 64;

        public static JsonValue Parse(string text)
        {
            if (text == null)
            {
                throw new JsonParseException("input is null", 0);
            }

            var cursor = 0;
            // UTF-8のBOMは取り除く。fetch().json() のUTF-8デコードと同じ扱いにする。
            if (text.Length > 0 && text[0] == '\uFEFF')
            {
                cursor = 1;
            }

            SkipWhitespace(text, ref cursor);
            var value = ParseValue(text, ref cursor, 0);
            SkipWhitespace(text, ref cursor);
            if (cursor != text.Length)
            {
                throw new JsonParseException("unexpected trailing content", cursor);
            }

            return value;
        }

        private static JsonValue ParseValue(string text, ref int cursor, int depth)
        {
            if (depth > MaxDepth)
            {
                throw new JsonParseException("nesting is too deep", cursor);
            }

            if (cursor >= text.Length)
            {
                throw new JsonParseException("unexpected end of input", cursor);
            }

            switch (text[cursor])
            {
                case '{':
                    return ParseObject(text, ref cursor, depth);
                case '[':
                    return ParseArray(text, ref cursor, depth);
                case '"':
                    return JsonValue.FromString(ParseString(text, ref cursor));
                case 't':
                    Expect(text, ref cursor, "true");
                    return JsonValue.True;
                case 'f':
                    Expect(text, ref cursor, "false");
                    return JsonValue.False;
                case 'n':
                    Expect(text, ref cursor, "null");
                    return JsonValue.Null;
                default:
                    return JsonValue.FromNumber(ParseNumber(text, ref cursor));
            }
        }

        private static JsonValue ParseObject(string text, ref int cursor, int depth)
        {
            cursor++; // 開き波括弧
            var members = new Dictionary<string, JsonValue>();
            SkipWhitespace(text, ref cursor);
            if (Peek(text, cursor) == '}')
            {
                cursor++;
                return JsonValue.FromObject(members);
            }

            while (true)
            {
                SkipWhitespace(text, ref cursor);
                if (Peek(text, cursor) != '"')
                {
                    throw new JsonParseException("object key must be a string", cursor);
                }

                var key = ParseString(text, ref cursor);
                SkipWhitespace(text, ref cursor);
                if (Peek(text, cursor) != ':')
                {
                    throw new JsonParseException("colon is missing after an object key", cursor);
                }

                cursor++;
                SkipWhitespace(text, ref cursor);
                members[key] = ParseValue(text, ref cursor, depth + 1);
                SkipWhitespace(text, ref cursor);

                var next = Peek(text, cursor);
                if (next == ',')
                {
                    cursor++;
                    continue;
                }

                if (next == '}')
                {
                    cursor++;
                    return JsonValue.FromObject(members);
                }

                throw new JsonParseException("comma or closing brace is missing in an object", cursor);
            }
        }

        private static JsonValue ParseArray(string text, ref int cursor, int depth)
        {
            cursor++; // 開き角括弧
            var items = new List<JsonValue>();
            SkipWhitespace(text, ref cursor);
            if (Peek(text, cursor) == ']')
            {
                cursor++;
                return JsonValue.FromArray(items);
            }

            while (true)
            {
                SkipWhitespace(text, ref cursor);
                items.Add(ParseValue(text, ref cursor, depth + 1));
                SkipWhitespace(text, ref cursor);

                var next = Peek(text, cursor);
                if (next == ',')
                {
                    cursor++;
                    continue;
                }

                if (next == ']')
                {
                    cursor++;
                    return JsonValue.FromArray(items);
                }

                throw new JsonParseException("comma or closing bracket is missing in an array", cursor);
            }
        }

        private static string ParseString(string text, ref int cursor)
        {
            cursor++; // 開き二重引用符
            var builder = new StringBuilder();
            while (true)
            {
                if (cursor >= text.Length)
                {
                    throw new JsonParseException("string is not terminated", cursor);
                }

                var c = text[cursor];
                if (c == '"')
                {
                    cursor++;
                    return builder.ToString();
                }

                if (c == '\\')
                {
                    cursor++;
                    if (cursor >= text.Length)
                    {
                        throw new JsonParseException("escape sequence is incomplete", cursor);
                    }

                    var escape = text[cursor];
                    cursor++;
                    switch (escape)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u': builder.Append(ParseHex4(text, ref cursor)); break;
                        default:
                            throw new JsonParseException("unknown escape sequence", cursor - 1);
                    }

                    continue;
                }

                if (c < 0x20)
                {
                    throw new JsonParseException("raw control character in a string", cursor);
                }

                builder.Append(c);
                cursor++;
            }
        }

        private static char ParseHex4(string text, ref int cursor)
        {
            if (cursor + 4 > text.Length)
            {
                throw new JsonParseException("unicode escape is incomplete", cursor);
            }

            var code = 0;
            for (var i = 0; i < 4; i++)
            {
                var digit = HexDigit(text[cursor + i]);
                if (digit < 0)
                {
                    throw new JsonParseException("unicode escape is not hexadecimal", cursor + i);
                }

                code = (code << 4) | digit;
            }

            cursor += 4;
            // サロゲートはそのまま積む。対になっていれば .NET の文字列として正しく結合される。
            return (char)code;
        }

        private static int HexDigit(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }

            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }

            return -1;
        }

        private static double ParseNumber(string text, ref int cursor)
        {
            var start = cursor;
            if (Peek(text, cursor) == '-')
            {
                cursor++;
            }

            // 整数部。先頭が 0 のときは数字を続けられない。
            if (Peek(text, cursor) == '0')
            {
                cursor++;
            }
            else if (IsDigit(Peek(text, cursor)))
            {
                while (IsDigit(Peek(text, cursor)))
                {
                    cursor++;
                }
            }
            else
            {
                throw new JsonParseException("number is malformed", cursor);
            }

            if (Peek(text, cursor) == '.')
            {
                cursor++;
                if (!IsDigit(Peek(text, cursor)))
                {
                    throw new JsonParseException("fraction has no digit", cursor);
                }

                while (IsDigit(Peek(text, cursor)))
                {
                    cursor++;
                }
            }

            var exponent = Peek(text, cursor);
            if (exponent == 'e' || exponent == 'E')
            {
                cursor++;
                var sign = Peek(text, cursor);
                if (sign == '+' || sign == '-')
                {
                    cursor++;
                }

                if (!IsDigit(Peek(text, cursor)))
                {
                    throw new JsonParseException("exponent has no digit", cursor);
                }

                while (IsDigit(Peek(text, cursor)))
                {
                    cursor++;
                }
            }

            var literal = text.Substring(start, cursor - start);
            double parsed;
            if (!double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                throw new JsonParseException("number is out of range", start);
            }

            return parsed;
        }

        private static void Expect(string text, ref int cursor, string literal)
        {
            if (cursor + literal.Length > text.Length ||
                string.CompareOrdinal(text, cursor, literal, 0, literal.Length) != 0)
            {
                throw new JsonParseException("unknown literal", cursor);
            }

            cursor += literal.Length;
        }

        private static void SkipWhitespace(string text, ref int cursor)
        {
            while (cursor < text.Length)
            {
                var c = text[cursor];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                {
                    cursor++;
                    continue;
                }

                return;
            }
        }

        private static char Peek(string text, int cursor)
        {
            return cursor < text.Length ? text[cursor] : '\0';
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }
    }
}
