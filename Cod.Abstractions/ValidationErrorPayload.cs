namespace Cod
{
    public class ValidationErrorPayload : GenericErrorPayload
    {
        public ValidationState Validation { get; set; }
    }
}
