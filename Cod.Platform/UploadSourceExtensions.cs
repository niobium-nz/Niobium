using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cod.Platform
{
    public static class UploadSourceExtensions
    {
        public static async Task<OperationResult<Uri>> GetMediaUrlAsync(this UploadSource uploadSource, IBrandService brandService, WechatIntegration wechatIntegration)
        {
            switch (uploadSource.OpenIDKind)
            {
                case OpenIDKind.Wechat:
                    var bi = await brandService.GetAsync(OpenIDKind.Wechat, uploadSource.AppID);
                    if (bi == null)
                    {
                        return OperationResult<Uri>.Create(InternalError.InternalServerError, null);
                    }

                    var secert = bi.WechatSecret;
                    var rs = await wechatIntegration.GenerateMediaUri(uploadSource.AppID, secert, uploadSource.FileID);
                    if (!rs.IsSuccess)
                    {
                        return OperationResult<Uri>.Create(InternalError.InternalServerError, JsonConvert.SerializeObject(rs));
                    }
                    else
                    {
                        return OperationResult<Uri>.Create(rs.Result);
                    }

                default:
                    return OperationResult<Uri>.Create(InternalError.InternalServerError, null);
            }
        }
    }
}
