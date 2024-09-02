using Cod.Platform.Tenant;
using Cod.Platform.Tenant.Wechat;

namespace Cod.Storage.Blob.Tencent.Wechat
{
    public static class UploadSourceExtensions
    {
        public static async Task<OperationResult<Uri>> GetMediaUrlAsync(this UploadSource uploadSource, IBrandService brandService, WechatIntegration wechatIntegration)
        {
            switch (uploadSource.OpenIDKind)
            {
                case OpenIDKind.Wechat:
                    BrandingInfo bi = await brandService.GetAsync(OpenIDKind.Wechat, uploadSource.AppID);
                    if (bi == null)
                    {
                        return new OperationResult<Uri>(InternalError.InternalServerError);
                    }

                    string secert = bi.WechatSecret;
                    OperationResult<Uri> rs = await wechatIntegration.GenerateMediaUri(uploadSource.AppID, secert, uploadSource.FileID);
                    return !rs.IsSuccess ? new OperationResult<Uri>(rs) : new OperationResult<Uri>(rs.Result);

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
