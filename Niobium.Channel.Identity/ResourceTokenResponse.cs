using System.Net;

namespace Niobium.Channel.Identity
{
    public class ResourceTokenResponse
    {
        public StorageSignature? Token { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }
}
