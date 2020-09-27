namespace Cod
{
    public static class ValidationStateExtensions
    {
        public static OperationResult ToOperationResult(this ValidationState validationState)
            => new OperationResult(InternalError.BadRequest) { Reference = validationState.ToDictionary() };
    }
}
