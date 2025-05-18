using System.Net;

namespace Cod.Channel.Identity
{
    public class ResourceTokenResponse
    {
        public StorageSignature? Token { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }
}
