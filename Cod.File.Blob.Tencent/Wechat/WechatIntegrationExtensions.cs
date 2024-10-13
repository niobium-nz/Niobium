using Cod.Platform;
using Cod.Platform.Tenant.Wechat;
using Microsoft.Extensions.Logging;

namespace Cod.Storage.Blob.Tencent.Wechat
{
    public static class WechatIntegrationExtensions
    {
        public static async Task<OperationResult<WechatMediaSource>> GetMediaAsync(this WechatIntegration integration, string appId, string secret, string mediaID, int retry = 0)
        {
            if (retry >= 3)
            {
                return new OperationResult<WechatMediaSource>(InternalError.InternalServerError);
            }

            OperationResult<Uri> url = await integration.GenerateMediaUri(appId, secret, mediaID);
            if (!url.IsSuccess)
            {
                return new OperationResult<WechatMediaSource>(url);
            }

            Stream result = await url.Result.FetchStreamAsync(null, 1);
            if (result == null)
            {
                return new OperationResult<WechatMediaSource>(InternalError.GatewayTimeout);
            }

            if (result.Length > 128)
            {
                return new OperationResult<WechatMediaSource>(new WechatMediaSource
                {
                    MediaStream = result,
                    MediaUri = url.Result,
                });
            }

            using (StreamReader sr = new(result))
            {
                string err = await sr.ReadToEndAsync();
                if (err.Contains("\"errcode\":40001,"))
                {
                    await integration.RevokeAccessTokenAsync(appId);
                    return await integration.GetMediaAsync(appId, secret, mediaID, ++retry);
                }

                integration.Logger.LogError($"An error occurred while trying to download media {mediaID} from Wechat: {err}");
            }

            return new OperationResult<WechatMediaSource>(InternalError.BadGateway);
        }
    }
}
