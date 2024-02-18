namespace Cod.Platform.Stoarge.Wechat
{
    public class WechatMediaSource : IDisposable
    {
        public Uri MediaUri { get; set; }

        public Stream MediaStream { get; set; }

        public void Dispose()
        {
            if (MediaStream != null)
            {
                MediaStream.Dispose();
                MediaStream = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
