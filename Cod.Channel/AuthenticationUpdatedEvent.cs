namespace Cod.Channel
{
    public class AuthenticationUpdatedEvent
    {
        public bool IsAuthenticated { get; }

        public AuthenticationUpdatedEvent(bool isAuthenticated)
        {            
            IsAuthenticated = isAuthenticated;
        }
    }
}
