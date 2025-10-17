using System.Text;

namespace Niobium.Messaging.ServiceBus
{
    internal sealed class StringHelper
    {
        public static string ToSnakeCase(string text)
        {
            if (text.Length < 2)
            {
                return text.ToLowerInvariant();
            }

            StringBuilder sb = new();
            sb.Append(char.ToLowerInvariant(text[0]));
            for (int i = 1; i < text.Length; ++i)
            {
                char c = text[i];
                if (char.IsUpper(c))
                {
                    sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string ToSnakeCaseKeepCommonAbbreviations(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(text.Length * 2);
            sb.Append(char.ToUpperInvariant(text[0]));

            for (int i = 1; i < text.Length; i++)
            {
                char current = text[i];
                char previous = text[i - 1];
                char? next = i + 1 < text.Length ? text[i + 1] : (char?)null;

                bool isUpper = char.IsUpper(current);
                bool prevIsUpper = char.IsUpper(previous);
                bool prevIsLower = char.IsLower(previous);
                bool nextIsLower = next.HasValue && char.IsLower(next.Value);

                if (isUpper && (prevIsLower || (prevIsUpper && nextIsLower)))
                {
                    sb.Append('_');
                }

                sb.Append(char.ToUpperInvariant(current));
            }

            return sb.ToString();
        }
    }
}
