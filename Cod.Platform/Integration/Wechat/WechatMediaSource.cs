using System;
using System.IO;

namespace Cod.Platform
{
    public class WechatMediaSource : IDisposable
    {
        public Uri MediaUri { get; set; }

        public Stream MediaStream { get; set; }

        public void Dispose()
        {
            if (this.MediaStream != null)
            {
                this.MediaStream.Dispose();
                this.MediaStream = null;
            }
        }
    }
}
