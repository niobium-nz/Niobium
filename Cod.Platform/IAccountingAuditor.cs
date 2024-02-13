using Cod.Platform.Entity;

namespace Cod.Platform
{
    public interface IAccountingAuditor
    {
        Task AuditAsync(Accounting accounting, IEnumerable<Transaction> transactions);
    }
}
