namespace Cod.Channel
{
    public class LoadCommandResult<TDomain>
    {
        public required IEnumerable<TDomain> DomainsLoaded { get; set; }
    }
}
