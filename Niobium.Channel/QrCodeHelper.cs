namespace Niobium.Channel
{
    public static class QrCodeHelper
    {
        public static object[] GetQrCodeParameters(string containerID, string href, int width, int height)
        {
            return [
            containerID,
            href,
            width,
            height,
        ];
        }
    }
}
