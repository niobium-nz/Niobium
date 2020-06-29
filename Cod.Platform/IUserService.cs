using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IUserService
    {
        Task<UserDomain> GetOrCreateAsync(string mobile, string remoteIP);

        Task<UserDomain> GetOrCreateAsync(string mobile, OpenIDKind kind, string appID, string openID, string remoteIP);
    }
}
