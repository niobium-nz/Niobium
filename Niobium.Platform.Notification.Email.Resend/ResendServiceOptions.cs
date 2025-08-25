namespace Niobium.Platform.Notification.Email.Resend
{
    public class ResendServiceOptions
    {
        public required string GlobalAPIKey { get; set; }

        public required Dictionary<string, string> DomainScopedAPIKeys { get; set; }

        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(GlobalAPIKey) || (DomainScopedAPIKeys != null && DomainScopedAPIKeys.Count > 0);
        }
    }
}