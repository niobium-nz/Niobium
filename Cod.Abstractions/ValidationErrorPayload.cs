namespace Cod
{
    public class ValidationErrorPayload : GenericErrorPayload
    {
        public required ValidationState Validation { get; set; }
    }
}
