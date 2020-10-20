using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IPaymentProcessor
    {
        Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request);

        Task<OperationResult<ChargeResult>> ReportAsync(object notification);
    }
}
