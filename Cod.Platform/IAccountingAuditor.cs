using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public interface IAccountingAuditor
    {
        Task AuditAsync(Accounting accounting, IEnumerable<Transaction> transactions, ILogger logger);
    }
}
