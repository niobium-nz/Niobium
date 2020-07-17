namespace Cod
{
    public static class FileSizeFormater
    {
        public static string FormatFileSize(this long size)
        {
            var result = size / 1d;
            if (result < 1024)
            {
                return $"{result} B";
            }

            result /= 1024d;
            if (result<1024)
            {
                return $"{result} KB";
            }

            result /= 1024d;
            if (result < 1024)
            {
                return $"{result} MB";
            }

            result /= 1024d;
            return $"{result} GB";
        }
    }
}
