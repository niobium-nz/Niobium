namespace Cod.Platform.Finance
{
    public interface IPaymentService
    {
        Task<OperationResult<ChargeResult>> RetrieveChargeAsync(string transaction, PaymentChannels paymentChannel);

        Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request);

        Task<OperationResult<ChargeResult>> ReportAsync(object notification);
    }
}
