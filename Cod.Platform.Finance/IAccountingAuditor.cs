using Cod.Finance;

namespace Cod.Platform.Finance
{
    public interface IAccountingAuditor
    {
        Task AuditAsync(Accounting accounting, IEnumerable<Transaction> transactions);
    }
}
