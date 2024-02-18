using Cod.Platform.Tenants;
using Cod.Platform.Tenants.Wechat;
using Microsoft.Extensions.Logging;

namespace Cod.Platform.Finance.WechatPay
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
                return new OperationResult<ChargeResponse>(Cod.InternalError.NotAcceptable);
            }

            WechatPayer payer = (WechatPayer)request.Account;
            string apiUri = await configuration.Value.GetSettingAsync<string>(Constant.API_URL);
            BrandingInfo branding = await brandService.Value.GetAsync(OpenIDKind.Wechat, payer.AppID);
            string attach = request.Reference ?? WechatChargeNotification.BuildAttach(request.TargetKind, request.Target);
            OperationResult<string> prepayid = await WechatIntegration.JSAPIPay(
                payer.OpenID,
                request.Amount,
                payer.AppID,
                request.Source,
                string.Concat(request.Order.Where(char.IsLetterOrDigit)), // REMARK (5he11) 微信虽然接受订单号内容可以是任意字符，但是长度有限制，所以过滤掉无用字符，缩短长度
                request.Description,
                attach,
                request.IP,
                branding.WechatMerchantID,
                $"{apiUri}/v1/wechat/notifications",
                branding.WechatMerchantSignature);
            if (!prepayid.IsSuccess)
            {
                logger.LogError($"支付通道上游返回错误: {prepayid.Message} 参考: {prepayid.Reference}");
                return new OperationResult<ChargeResponse>(prepayid);
            }
            else
            {
                Dictionary<string, object> paySignature = WechatIntegration.GetJSAPIPaySignature(prepayid.Result, payer.AppID, branding.WechatMerchantSignature);
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

        public async Task<OperationResult<ChargeResult>> ReportAsync(object notification)
        {
            if (notification is WechatChargeNotification wechatChargeNotification)
            {
                BrandingInfo branding = await brandService.Value.GetAsync(OpenIDKind.Wechat, wechatChargeNotification.AppID);
                return !wechatChargeNotification.Validate(branding.WechatMerchantSignature)
                    ? new OperationResult<ChargeResult>(Cod.InternalError.Forbidden)
                    : new OperationResult<ChargeResult>(new ChargeResult
                    {
                        Account = new WechatPayer
                        {
                            AppID = wechatChargeNotification.AppID,
                            OpenID = wechatChargeNotification.Account,
                            OpenIDKind = OpenIDKind.Wechat,
                        },
                        Amount = wechatChargeNotification.Amount,
                        Channel = PaymentChannels.Wechat,
                        Currency = Constant.CNY,
                        PaymentKind = PaymentKind.Charge,
                        Reference = wechatChargeNotification.Attach,
                        Source = wechatChargeNotification.Device,
                        Target = wechatChargeNotification.GetTarget(),
                        TargetKind = wechatChargeNotification.GetKind(),
                        UpstreamID = wechatChargeNotification.Reference,
                        AuthorizedAt = wechatChargeNotification.Paid,
                    });
            }

            return new OperationResult<ChargeResult>(Cod.InternalError.NotAcceptable);
        }

        private static bool Support(ChargeRequest request)
        {
            return request != null && request.Channel == PaymentChannels.Wechat;
        }

        public Task<OperationResult<ChargeResult>> RetrieveChargeAsync(string transaction, PaymentChannels paymentChannels)
        {
            return paymentChannels != PaymentChannels.Wechat
                ? Task.FromResult(new OperationResult<ChargeResult>(Cod.InternalError.NotAcceptable))
                : throw new NotSupportedException();
        }
    }
}
