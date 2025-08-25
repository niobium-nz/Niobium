namespace Niobium
{
    public static class FileSizeFormater
    {
        public static string FormatFileSize(this long size)
        {
            double result = size / 1d;
            if (result < 1024)
            {
                return $"{Math.Round(result, 0)} B";
            }

            result /= 1024d;
            if (result < 1024)
            {
                return $"{Math.Round(result, 0)} KB";
            }

            result /= 1024d;
            if (result < 1024)
            {
                return $"{Math.Round(result, 2)} MB";
            }

            result /= 1024d;
            return $"{Math.Round(result, 2)} GB";
        }
    }
}
