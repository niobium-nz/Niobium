namespace Cod.Channel
{
    public class LoadCommandResult<TDomain>
    {
        public IEnumerable<TDomain> DomainsLoaded { get; set; }
    }
}
