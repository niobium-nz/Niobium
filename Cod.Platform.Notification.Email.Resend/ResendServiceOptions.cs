namespace Cod.Platform.Notification.Email.Resend
{
    public class ResendServiceOptions
    {
        public required string APIKey { get; set; }

        public bool Validate() => !string.IsNullOrWhiteSpace(APIKey);
    }
}