using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cinnarios.Launch
{
    public sealed class PhoneShopException : Exception
    {
        public int Status { get; }
        public string Code { get; }

        public PhoneShopException(int status, string code, string message) : base(message)
        {
            Status = status;
            Code = code;
        }
    }

    /// <summary>Reads the small JSON bodies the shop posts, and writes JSON the pages already understand.</summary>
    public static class PhoneShopJson
    {
        public static object Parse(string text)
        {
            int index = 0;
            return Read(text, ref index);
        }

        public static string Quote(string value)
        {
            if (value == null)
            {
                return "null";
            }

            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            foreach (char character in value)
            {
                if (character == '"' || character == '\\') builder.Append('\\').Append(character);
                else if (character == '\n') builder.Append("\\n");
                else if (character == '\r') builder.Append("\\r");
                else builder.Append(character);
            }

            builder.Append('"');
            return builder.ToString();
        }

        public static string Error(string code, string message)
        {
            return "{\"code\":" + Quote(code) + ",\"message\":" + Quote(message) + "}";
        }

        private static object Read(string text, ref int index)
        {
            Skip(text, ref index);
            if (index >= text.Length) return null;
            char character = text[index];
            if (character == '{') return ReadObject(text, ref index);
            if (character == '[') return ReadArray(text, ref index);
            if (character == '"') return ReadString(text, ref index);
            if (character == 't' || character == 'f') return ReadBool(text, ref index);
            if (character == 'n')
            {
                index += 4;
                return null;
            }

            return ReadNumber(text, ref index);
        }

        private static Dictionary<string, object> ReadObject(string text, ref int index)
        {
            var result = new Dictionary<string, object>();
            index++;
            Skip(text, ref index);
            if (index < text.Length && text[index] == '}')
            {
                index++;
                return result;
            }

            while (index < text.Length)
            {
                Skip(text, ref index);
                string key = ReadString(text, ref index);
                Skip(text, ref index);
                index++;
                result[key] = Read(text, ref index);
                Skip(text, ref index);
                if (index < text.Length && text[index] == ',')
                {
                    index++;
                    continue;
                }

                index++;
                break;
            }

            return result;
        }

        private static List<object> ReadArray(string text, ref int index)
        {
            var result = new List<object>();
            index++;
            Skip(text, ref index);
            if (index < text.Length && text[index] == ']')
            {
                index++;
                return result;
            }

            while (index < text.Length)
            {
                result.Add(Read(text, ref index));
                Skip(text, ref index);
                if (index < text.Length && text[index] == ',')
                {
                    index++;
                    continue;
                }

                index++;
                break;
            }

            return result;
        }

        private static string ReadString(string text, ref int index)
        {
            index++;
            var builder = new StringBuilder();
            while (index < text.Length)
            {
                char character = text[index++];
                if (character == '"') return builder.ToString();
                if (character != '\\')
                {
                    builder.Append(character);
                    continue;
                }

                char escaped = text[index++];
                builder.Append(escaped == 'n' ? '\n' : escaped == 'r' ? '\r' : escaped);
            }

            return builder.ToString();
        }

        private static bool ReadBool(string text, ref int index)
        {
            bool value = text[index] == 't';
            index += value ? 4 : 5;
            return value;
        }

        private static double ReadNumber(string text, ref int index)
        {
            int start = index;
            while (index < text.Length && "0123456789+-.eE".IndexOf(text[index]) >= 0) index++;
            return double.Parse(text.Substring(start, index - start), CultureInfo.InvariantCulture);
        }

        private static void Skip(string text, ref int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
        }
    }
}
