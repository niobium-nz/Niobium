using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    internal class PaymentService : IPaymentService
    {
        private readonly Lazy<IEnumerable<IPaymentProcessor>> processors;

        public PaymentService(Lazy<IEnumerable<IPaymentProcessor>> processors) => this.processors = processors;

        public async Task<OperationResult<ChargeResult>> RetrieveChargeAsync(string transaction, PaymentChannels paymentChannel)
        {
            foreach (var processor in this.processors.Value)
            {
                var result = await processor.RetrieveChargeAsync(transaction, paymentChannel);
                if (result.Code == InternalError.NotAcceptable)
                {
                    continue;
                }

                return result;
            }

            throw new NotSupportedException();
        }

        public async Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request)
        {
            foreach (var processor in this.processors.Value)
            {
                var result = await processor.ChargeAsync(request);
                if (result.Code == InternalError.NotAcceptable)
                {
                    continue;
                }

                return result;
            }

            throw new NotSupportedException();
        }

        public async Task<OperationResult<ChargeResult>> ReportAsync(object notification)
        {
            foreach (var processor in this.processors.Value)
            {
                var result = await processor.ReportAsync(notification);
                if (result.Code == InternalError.NotAcceptable)
                {
                    continue;
                }
                return result;
            }
            throw new NotSupportedException();
        }
    }
}
