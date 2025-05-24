namespace Cod.Channel
{
    public static class QrCodeHelper
    {
        public static object[] GetQrCodeParameters(string containerID, string href, int width, int height) =>
        [
            containerID,
            href,
            width,
            height,
        ];
    }
}
