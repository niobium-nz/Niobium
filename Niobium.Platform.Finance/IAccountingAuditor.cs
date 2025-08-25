using Niobium.Finance;

namespace Niobium.Platform.Finance
{
    public interface IAccountingAuditor
    {
        Task AuditAsync(Accounting accounting, IEnumerable<Transaction> transactions);
    }
}
