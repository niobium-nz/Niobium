namespace Cod
{
    public static class ValidationStateExtensions
    {
        public static OperationResult ToOperationResult(this ValidationState validationState)
            => new OperationResult(InternalError.BadRequest) { Reference = validationState.ToDictionary() };

        public static OperationResult<T> ToOperationResult<T>(this ValidationState validationState)
            => new OperationResult<T>(InternalError.BadRequest) { Reference = validationState.ToDictionary() };
    }
}
