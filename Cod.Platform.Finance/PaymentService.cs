using Cod.Finance;

namespace Cod.Platform.Finance
{
    public class PaymentService(Lazy<IEnumerable<IPaymentProcessor>> processors, IEnumerable<IDomainEventHandler<IDomain<Transaction>>> eventHandlers) 
        : IPaymentService
    {
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

        public virtual async Task<OperationResult<ChargeResult>> ReportAsync(string notificationJSON)
        {
            foreach (IPaymentProcessor processor in processors.Value)
            {
                OperationResult<ChargeResult> result = await processor.ReportAsync(notificationJSON);
                if (result.Code == Cod.InternalError.NotAcceptable)
                {
                    continue;
                }

                if (result.IsSuccess && result.Result?.Transaction != null)
                {
                    await eventHandlers.InvokeAsync(new TransactionCreatedEvent(result.Result.Transaction));
                }

                return result;
            }
            throw new NotSupportedException();
        }
    }
}
