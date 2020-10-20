using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IPaymentService
    {
        Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request);

        Task<OperationResult<ChargeResult>> ReportAsync(object notification);
    }
}
