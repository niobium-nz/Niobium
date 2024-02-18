namespace Cod
{
    public static class BCC
    {
        public static byte CalculateBCC(this byte[] buff, int length)
        {
            byte bcc = 0;

            if (buff != null && buff.Length > 0 && buff.Length >= length)
            {
                for (int i = 0; i < length; i++)
                {
                    bcc ^= buff[i];
                }
            }

            return bcc;
        }
    }
}
