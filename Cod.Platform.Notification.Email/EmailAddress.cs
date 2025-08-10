namespace Cod.Platform.Notification.Email
{
    public class EmailAddress
    {
        public string? DisplayName { get; set; }

        public required string Address { get; set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(DisplayName) ? Address : $"{DisplayName} <{Address}>";
        }
    }
}
