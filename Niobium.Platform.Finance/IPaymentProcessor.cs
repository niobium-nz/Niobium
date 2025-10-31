using Niobium.Finance;

namespace Niobium.Platform.Finance
{
    public interface IPaymentProcessor
    {
        Task<OperationResult<ChargeResult>> RetrieveChargeAsync(string tenant, string transaction, PaymentChannels paymentChannel);

        Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request);

        Task<OperationResult<ChargeResult>> ReportAsync(string tenant, string notificationJSON);
    }
}
