using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Platform.Model;

namespace Cod.Platform
{
    public interface IAccountingAuditor
    {
        Task AuditAsync(Accounting accounting, IEnumerable<Transaction> transactions);
    }
}
