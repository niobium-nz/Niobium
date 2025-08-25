namespace Niobium
{
    public class GenericErrorPayload
    {
        public int Code { get; set; }

        public required string Message { get; set; }

        public object? Reference { get; set; }
    }
}
