using System;
using System.Linq;
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

            // REMARK (5he11) 微信虽然接受订单号内容可以是任意字符，但是长度有限制，所以过滤掉无用字符，缩短长度
            request.Order = String.Concat(request.Order.Where(Char.IsLetterOrDigit));

            var payer = (WechatPayer)request.Account;
            var apiUri = await this.configuration.Value.GetSettingAsync<string>(Constant.API_URL);
            var branding = await this.brandService.Value.GetAsync(OpenIDKind.Wechat, payer.AppID);
            var attach = request.Reference ?? WechatChargeNotification.BuildAttach(request.TargetKind, request.Target);
            var prepayid = await this.wechatIntegration.Value.JSAPIPay(
                payer.OpenID,
                request.Amount,
                payer.AppID,
                request.Source,
                request.Order,
                request.Description,
                attach,
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

        public async Task<OperationResult<ChargeResult>> ReportAsync(object notification)
        {
            if (notification is WechatChargeNotification wechatChargeNotification)
            {
                var branding = await brandService.Value.GetAsync(OpenIDKind.Wechat, wechatChargeNotification.AppID);
                if (!wechatChargeNotification.Validate(branding.WechatMerchantSignature))
                {
                    return new OperationResult<ChargeResult>(InternalError.Forbidden);
                }

                return new OperationResult<ChargeResult>(new ChargeResult
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

            throw new NotSupportedException();
        }

        private static bool Support(ChargeRequest request) => request != null && request.Channel == PaymentChannels.Wechat;
    }
}
