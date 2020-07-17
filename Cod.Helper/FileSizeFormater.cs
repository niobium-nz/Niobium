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
                return $"{result} Kb";
            }

            result /= 1024d;
            if (result < 1024)
            {
                return $"{result} Mb";
            }

            result /= 1024d;
            return $"{result} Gb";
        }
    }
}
