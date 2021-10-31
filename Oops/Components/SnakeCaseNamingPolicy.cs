using System.Text.Json;
using System.Text;
using System;

namespace Oops.Components
{
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            var chars = name.ToCharArray();
            return UnderLineString(chars);
        }

        private static string UnderLineString(Span<char> chars)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(Char.ToLower(chars[0]));
            for (int i = 1; i < chars.Length; i++)
            {
                if (Char.IsUpper(chars[i]))
                    stringBuilder.Append($"_");

                stringBuilder.Append(Char.ToLower(chars[i]));
            }
            return stringBuilder.ToString();
        }
    }
}
