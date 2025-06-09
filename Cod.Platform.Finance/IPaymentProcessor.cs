namespace Cod.Platform.Finance
{
    public interface IPaymentProcessor
    {
        Task<OperationResult<ChargeResult>> RetrieveChargeAsync(string transaction, PaymentChannels paymentChannel);

        Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request);

        Task<OperationResult<ChargeResult>> ReportAsync(string notificationJSON);
    }
}
