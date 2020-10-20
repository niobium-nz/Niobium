using System;
using System.Threading.Tasks;
using Cod.Platform.Integration.Wechat;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    internal class WechatPaymentProcessor : IPaymentProcessor
    {
        private readonly Lazy<IBrandService> brandService;
        private readonly Lazy<IConfigurationProvider> configuration;
        private readonly Lazy<WechatIntegration> wechatIntegration;
        private readonly ILogger logger;

        public WechatPaymentProcessor(
            Lazy<IBrandService> brandService,
            Lazy<IConfigurationProvider> configuration,
            Lazy<WechatIntegration> wechatIntegration,
            ILogger logger)
        {
            this.brandService = brandService;
            this.configuration = configuration;
            this.wechatIntegration = wechatIntegration;
            this.logger = logger;
        }

        public async Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request)
        {
            if (!Support(request))
            {
                return new OperationResult<ChargeResponse>(InternalError.NotAcceptable);
            }

            var payer = (WechatPayer)request.Account;
            var apiUri = await this.configuration.Value.GetSettingAsync<string>(Constant.API_URL);
            var branding = await this.brandService.Value.GetAsync(OpenIDKind.Wechat, payer.AppID);
            var reference = request.Reference ?? $"{(int)request.TargetKind}|{request.Target}";
            var prepayid = await this.wechatIntegration.Value.JSAPIPay(
                payer.OpenID,
                request.Amount,
                payer.AppID,
                request.Source,
                request.Order,
                request.Description,
                reference,
                request.IP,
                branding.WechatMerchantID,
                $"{apiUri}/v1/wechat/notifications",
                branding.WechatMerchantSignature);
            if (!prepayid.IsSuccess)
            {
                this.logger.LogError($"支付通道上游返回错误: {prepayid.Message} 参考: {prepayid.Reference}");
                return new OperationResult<ChargeResponse>(prepayid);
            }
            else
            {
                var paySignature = this.wechatIntegration.Value.GetJSAPIPaySignature(prepayid.Result, payer.AppID, branding.WechatMerchantSignature);
                return new OperationResult<ChargeResponse>(
                    new ChargeResponse
                    {
                        Amount = request.Amount,
                        Method = PaymentMethodKind.WechatJSAPI,
                        Reference = request.Order,
                        Extra = request.Source,
                        UpstreamID = prepayid.Result,
                        Instruction = paySignature,
                    });
            }
        }

        public Task<OperationResult<ChargeResult>> ReportAsync(object notification) => throw new NotImplementedException();

        private static bool Support(ChargeRequest request) => request != null && request.Channel == PaymentChannels.Wechat;
    }
}
