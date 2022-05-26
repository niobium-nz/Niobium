namespace AzureFunctions.Autofac.Exceptions
{
    public class MissingAttributeException : Exception
    {
        public MissingAttributeException() : base() { }
        public MissingAttributeException(string message) : base(message) { }
        public MissingAttributeException(string message, Exception innerException) : base(message, innerException) { }
    }
}
