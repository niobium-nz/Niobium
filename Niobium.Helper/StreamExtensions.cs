namespace Niobium
{
    public static class StreamExtensions
    {
        public static byte[] ToByteArray(this Stream input)
        {
            if (input.CanSeek)
            {
                input.Seek(0, SeekOrigin.Begin);
            }

            if (input is MemoryStream m)
            {
                return m.ToArray();
            }

            byte[] data;
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                data = ms.ToArray();
            }
            return data;
        }
    }
}
