using System;
using System.Globalization;
using System.Text;

namespace Cod
{
    public static class StringHelper
    {
        private static readonly Encoding encoding = Encoding.GetEncoding(936);
        private static readonly int[] areacode = { 45217, 45253, 45761, 46318, 46826, 47010, 47297, 47614, 48119, 48119, 49062, 49324, 49896, 50371, 50614, 50622, 50906, 51387, 51446, 52218, 52698, 52698, 52698, 52980, 53689, 54481 };

        public static string GetChineseInitial(string cnChar)
        {
            if (cnChar.Length != 1)
            {
                throw new NotSupportedException();
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            byte[] arrCN = encoding.GetBytes(cnChar);
            if (arrCN.Length > 1)
            {
                int area = (short)arrCN[0];
                int pos = (short)arrCN[1];
                int code = (area << 8) + pos;

                for (int i = 0; i < 26; i++)
                {
                    int max = 55290;

                    if (i != 25)
                    {
                        max = areacode[i + 1];
                    }

                    if (areacode[i] <= code && code < max)
                    {
                        return encoding.GetString(new byte[] { (byte)(65 + i) });
                    }
                }

                return "*";
            }
            else
            {
                return cnChar;
            }
        }

        public static bool IsNumberCharacter(char input) => char.GetUnicodeCategory(input) == UnicodeCategory.DecimalDigitNumber;

        public static bool IsEnglishCharacter(char input) =>
            char.GetUnicodeCategory(input) == UnicodeCategory.LowercaseLetter
            || char.GetUnicodeCategory(input) == UnicodeCategory.UppercaseLetter;

        public static bool IsChineseCharacter(char input) => char.GetUnicodeCategory(input) == UnicodeCategory.OtherLetter;

        public static bool IsChineseOrEnglishCharacter(char input) => IsEnglishCharacter(input) || IsChineseCharacter(input);

        public static bool IsChineseOrEnglishOrNumberCharacter(char input) =>
            IsChineseOrEnglishCharacter(input) || IsNumberCharacter(input);

    }
}
