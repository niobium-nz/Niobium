namespace Niobium.Channel
{
    public interface IBrowser
    {
        Task<string> GetUserAgentAsync();

        Task<string> GetLocateAsync();
    }
}
