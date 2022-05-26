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
                        return new OperationResult<Uri>(InternalError.InternalServerError);
                    }

                    var secert = bi.WechatSecret;
                    var rs = await wechatIntegration.GenerateMediaUri(uploadSource.AppID, secert, uploadSource.FileID);
                    if (!rs.IsSuccess)
                    {
                        return new OperationResult<Uri>(rs);
                    }
                    else
                    {
                        return new OperationResult<Uri>(rs.Result);
                    }

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
