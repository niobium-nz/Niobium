using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Cod.Platform.Tenant
{
    public static class HttpRequestExtensions
    {
        public static async Task<OperationResult<T>> ValidateSignatureAndParseAsync<T>(this HttpRequest req, string secret, byte[] tenant = null)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new OperationResult<T>(Cod.InternalError.BadRequest);
            }

            T model = JsonSerializer.DeserializeObject<T>(requestBody);
            _ = model.TryValidate(out ValidationState validation);

            string requestSignature = null;
            if (req.Headers.TryGetValue("ETag", out StringValues etag))
            {
                requestSignature = etag.SingleOrDefault();
            }

            if (string.IsNullOrWhiteSpace(requestSignature))
            {
                validation.AddError("ETag", Localization.SignatureMissing);
            }

            if (!validation.IsValid)
            {
                return new OperationResult<T>(validation.ToOperationResult());
            }

            if (model is ITenantOwned tenantOwned)
            {
                tenant = tenantOwned.GetTenantAuthenticationIdentifier();
            }

            if (tenant == null)
            {
                throw new NotSupportedException();
            }

            string stringToSign = $"{req.Path.Value}?{requestBody}";
            string tenantSecret = Wechat.SignatureHelper.GetTenantSecret(tenant, secret);
            string signature = SignatureHelper.GetSignature(stringToSign, tenantSecret);
            return signature.ToUpperInvariant() != requestSignature.ToUpperInvariant()
                ? new OperationResult<T>(Cod.InternalError.AuthenticationRequired)
                : new OperationResult<T>(model);
        }

    }
}
