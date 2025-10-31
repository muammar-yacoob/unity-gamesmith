using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Simple JSON serializer/deserializer for MCP communication
    /// Based on MiniJSON by Calvin Rien
    /// </summary>
    public static class MiniJSON
    {
        public static class Json
        {
            public static string Serialize(object obj)
            {
                return Serializer.Serialize(obj);
            }

            public static object Deserialize(string json)
            {
                if (json == null) return null;
                return Parser.Parse(json);
            }
        }

        sealed class Parser : IDisposable
        {
            const string WORD_BREAK = "{}[],:\"";
            StringReader json;

            Parser(string jsonString)
            {
                json = new StringReader(jsonString);
            }

            public static object Parse(string jsonString)
            {
                using (var instance = new Parser(jsonString))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                json.Dispose();
                json = null;
            }

            Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>();
                json.Read(); // {

                while (true)
                {
                    var nextChar = NextToken;
                    if (nextChar == TOKEN.NONE) return null;
                    if (nextChar == TOKEN.CURLY_CLOSE) { json.Read(); return table; }

                    string name = ParseString();
                    if (name == null) return null;
                    if (NextToken != TOKEN.COLON) return null;

                    json.Read();
                    table[name] = ParseValue();

                    nextChar = NextToken;
                    if (nextChar == TOKEN.COMMA) { json.Read(); continue; }
                    if (nextChar == TOKEN.CURLY_CLOSE) { json.Read(); return table; }
                    return null;
                }
            }

            List<object> ParseArray()
            {
                var array = new List<object>();
                json.Read(); // [

                while (true)
                {
                    var nextToken = NextToken;
                    if (nextToken == TOKEN.NONE) return null;
                    if (nextToken == TOKEN.SQUARED_CLOSE) { json.Read(); return array; }

                    object value = ParseValue();
                    array.Add(value);

                    nextToken = NextToken;
                    if (nextToken == TOKEN.COMMA) { json.Read(); continue; }
                    if (nextToken == TOKEN.SQUARED_CLOSE) { json.Read(); return array; }
                    return null;
                }
            }

            object ParseValue()
            {
                var nextToken = NextToken;
                switch (nextToken)
                {
                    case TOKEN.STRING: return ParseString();
                    case TOKEN.NUMBER: return ParseNumber();
                    case TOKEN.CURLY_OPEN: return ParseObject();
                    case TOKEN.SQUARED_OPEN: return ParseArray();
                    case TOKEN.TRUE: json.Read(); return true;
                    case TOKEN.FALSE: json.Read(); return false;
                    case TOKEN.NULL: json.Read(); return null;
                }
                return null;
            }

            string ParseString()
            {
                var s = new StringBuilder();
                json.Read(); // "

                bool parsing = true;
                while (parsing)
                {
                    if (json.Peek() == -1) break;

                    char c = NextChar;
                    switch (c)
                    {
                        case '"': parsing = false; break;
                        case '\\':
                            if (json.Peek() == -1) { parsing = false; break; }
                            c = NextChar;
                            switch (c)
                            {
                                case '"': s.Append('"'); break;
                                case '\\': s.Append('\\'); break;
                                case '/': s.Append('/'); break;
                                case 'b': s.Append('\b'); break;
                                case 'f': s.Append('\f'); break;
                                case 'n': s.Append('\n'); break;
                                case 'r': s.Append('\r'); break;
                                case 't': s.Append('\t'); break;
                                default: s.Append(c); break;
                            }
                            break;
                        default: s.Append(c); break;
                    }
                }
                return s.ToString();
            }

            object ParseNumber()
            {
                string number = NextWord;
                if (number.IndexOf('.') == -1)
                {
                    long parsedInt;
                    long.TryParse(number, out parsedInt);
                    return parsedInt;
                }
                double parsedDouble;
                double.TryParse(number, out parsedDouble);
                return parsedDouble;
            }

            void EatWhitespace()
            {
                while (char.IsWhiteSpace(PeekChar)) json.Read();
            }

            char PeekChar => Convert.ToChar(json.Peek());
            char NextChar => Convert.ToChar(json.Read());

            string NextWord
            {
                get
                {
                    var word = new StringBuilder();
                    while (!IsWordBreak(PeekChar))
                    {
                        word.Append(NextChar);
                        if (json.Peek() == -1) break;
                    }
                    return word.ToString();
                }
            }

            TOKEN NextToken
            {
                get
                {
                    EatWhitespace();
                    if (json.Peek() == -1) return TOKEN.NONE;

                    switch (PeekChar)
                    {
                        case '{': return TOKEN.CURLY_OPEN;
                        case '}': return TOKEN.CURLY_CLOSE;
                        case '[': return TOKEN.SQUARED_OPEN;
                        case ']': return TOKEN.SQUARED_CLOSE;
                        case ',': return TOKEN.COMMA;
                        case '"': return TOKEN.STRING;
                        case ':': return TOKEN.COLON;
                        case '0': case '1': case '2': case '3': case '4':
                        case '5': case '6': case '7': case '8': case '9':
                        case '-': return TOKEN.NUMBER;
                    }

                    switch (NextWord)
                    {
                        case "false": return TOKEN.FALSE;
                        case "true": return TOKEN.TRUE;
                        case "null": return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }

            bool IsWordBreak(char c) => char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;

            enum TOKEN
            {
                NONE, CURLY_OPEN, CURLY_CLOSE, SQUARED_OPEN, SQUARED_CLOSE,
                COLON, COMMA, STRING, NUMBER, TRUE, FALSE, NULL
            }
        }

        sealed class Serializer
        {
            StringBuilder builder;

            Serializer()
            {
                builder = new StringBuilder();
            }

            public static string Serialize(object obj)
            {
                var instance = new Serializer();
                instance.SerializeValue(obj);
                return instance.builder.ToString();
            }

            void SerializeValue(object value)
            {
                if (value == null) { builder.Append("null"); }
                else if (value is string) { SerializeString(value as string); }
                else if (value is bool) { builder.Append((bool)value ? "true" : "false"); }
                else if (value is IList) { SerializeArray(value as IList); }
                else if (value is IDictionary) { SerializeObject(value as IDictionary); }
                else if (value is char) { SerializeString(value.ToString()); }
                else { SerializeOther(value); }
            }

            void SerializeObject(IDictionary obj)
            {
                bool first = true;
                builder.Append('{');

                foreach (object e in obj.Keys)
                {
                    if (!first) builder.Append(',');
                    SerializeString(e.ToString());
                    builder.Append(':');
                    SerializeValue(obj[e]);
                    first = false;
                }

                builder.Append('}');
            }

            void SerializeArray(IList array)
            {
                builder.Append('[');
                bool first = true;

                foreach (object obj in array)
                {
                    if (!first) builder.Append(',');
                    SerializeValue(obj);
                    first = false;
                }

                builder.Append(']');
            }

            void SerializeString(string str)
            {
                builder.Append('\"');
                foreach (char c in str)
                {
                    switch (c)
                    {
                        case '"': builder.Append("\\\""); break;
                        case '\\': builder.Append("\\\\"); break;
                        case '\b': builder.Append("\\b"); break;
                        case '\f': builder.Append("\\f"); break;
                        case '\n': builder.Append("\\n"); break;
                        case '\r': builder.Append("\\r"); break;
                        case '\t': builder.Append("\\t"); break;
                        default: builder.Append(c); break;
                    }
                }
                builder.Append('\"');
            }

            void SerializeOther(object value)
            {
                if (value is float || value is double || value is decimal)
                {
                    builder.Append(Convert.ToDouble(value).ToString("R"));
                }
                else if (value is int || value is uint || value is long || value is sbyte ||
                         value is byte || value is short || value is ushort || value is ulong)
                {
                    builder.Append(value);
                }
                else
                {
                    SerializeString(value.ToString());
                }
            }
        }
    }
}
