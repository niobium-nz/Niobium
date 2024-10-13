using System.Text;

namespace Cod.Messaging.ServiceBus
{
    internal class StringHelper
    {
        public static string ToSnakeCase(string text)
        {
            if (text.Length < 2)
            {
                return text.ToLowerInvariant();
            }

            var sb = new StringBuilder();
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
    }
}
