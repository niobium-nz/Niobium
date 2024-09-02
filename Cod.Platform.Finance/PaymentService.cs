namespace Cod.Platform.Finance
{
    public class PaymentService : IPaymentService
    {
        private readonly Lazy<IEnumerable<IPaymentProcessor>> processors;

        public PaymentService(Lazy<IEnumerable<IPaymentProcessor>> processors)
        {
            this.processors = processors;
        }

        public virtual async Task<OperationResult<ChargeResult>> RetrieveChargeAsync(string transaction, PaymentChannels paymentChannel)
        {
            foreach (IPaymentProcessor processor in processors.Value)
            {
                OperationResult<ChargeResult> result = await processor.RetrieveChargeAsync(transaction, paymentChannel);
                if (result.Code == Cod.InternalError.NotAcceptable)
                {
                    continue;
                }

                return result;
            }

            throw new NotSupportedException();
        }

        public virtual async Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request)
        {
            foreach (IPaymentProcessor processor in processors.Value)
            {
                OperationResult<ChargeResponse> result = await processor.ChargeAsync(request);
                if (result.Code == Cod.InternalError.NotAcceptable)
                {
                    continue;
                }

                return result;
            }

            throw new NotSupportedException();
        }

        public virtual async Task<OperationResult<ChargeResult>> ReportAsync(object notification)
        {
            foreach (IPaymentProcessor processor in processors.Value)
            {
                OperationResult<ChargeResult> result = await processor.ReportAsync(notification);
                if (result.Code == Cod.InternalError.NotAcceptable)
                {
                    continue;
                }
                return result;
            }
            throw new NotSupportedException();
        }
    }
}
