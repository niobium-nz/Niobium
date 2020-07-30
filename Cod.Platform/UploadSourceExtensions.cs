using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public static class UploadSourceExtensions
    {
        public static async Task<OperationResult<string>> GetMediaUrlAsync(this UploadSource uploadSource, IBrandService brandService, WechatIntegration wechatIntegration)
        {
            switch (uploadSource.OpenIDKind)
            {
                case OpenIDKind.Wechat:
                    var x = await brandService.GetAsync(OpenIDKind.Wechat, uploadSource.AppID);
                    if (x == null)
                    {
                        return OperationResult<string>.Create(InternalError.InternalServerError, null);
                    }

                    var secert = x.WechatSecret;
                    var rs = await wechatIntegration.GenerateMediaDownloadUrl(uploadSource.AppID, secert, uploadSource.FileID);
                    if (!rs.IsSuccess)
                    {
                        return OperationResult<string>.Create(InternalError.InternalServerError, JsonConvert.SerializeObject(rs));
                    }
                    else
                    {
                        return OperationResult<string>.Create(rs.Result);
                    }

                default:
                    return OperationResult<string>.Create(InternalError.InternalServerError, null);
            }
        }
    }
}
