namespace Niobium
{
    public static class ValidationStateExtensions
    {
        public static OperationResult ToOperationResult(this ValidationState validationState)
        {
            return new OperationResult(InternalError.BadRequest) { Reference = validationState.ToDictionary() };
        }

        public static OperationResult<T> ToOperationResult<T>(this ValidationState validationState)
        {
            return new OperationResult<T>(InternalError.BadRequest) { Reference = validationState.ToDictionary() };
        }
    }
}
