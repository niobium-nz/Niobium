using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IDNSManager
    {
        bool Support(string domain, string serviceProvider);

        Task<OperationResult<IEnumerable<DNSRecord>>> QueryRecordsAsync(string domain);

        Task<OperationResult> CreateRecordAsync(string domain, string recordName, DNSRecordType type, string recordValue);

        Task<OperationResult> RemoveRecordAsync(string domain, string recordName, DNSRecordType type);
    }
}
