using System.Text;

namespace Cod
{
    public class OperationResult
    {
        public const int SuccessCode = 0;
        public static readonly OperationResult Success = new();
        public static readonly OperationResult BadRequest = new(InternalError.BadRequest);
        public static readonly OperationResult AuthenticationRequired = new(InternalError.AuthenticationRequired);
        public static readonly OperationResult PaymentRequired = new(InternalError.PaymentRequired);
        public static readonly OperationResult Forbidden = new(InternalError.Forbidden);
        public static readonly OperationResult NotFound = new(InternalError.NotFound);
        public static readonly OperationResult NotAllowed = new(InternalError.NotAllowed);
        public static readonly OperationResult NotAcceptable = new(InternalError.NotAcceptable);
        public static readonly OperationResult RequestTimeout = new(InternalError.RequestTimeout);
        public static readonly OperationResult Conflict = new(InternalError.Conflict);
        public static readonly OperationResult PreconditionFailed = new(InternalError.PreconditionFailed);
        public static readonly OperationResult Locked = new(InternalError.Locked);
        public static readonly OperationResult UpgradeRequired = new(InternalError.UpgradeRequired);
        public static readonly OperationResult TooManyRequests = new(InternalError.TooManyRequests);

        public static readonly OperationResult InternalServerError = new(InternalError.InternalServerError);
        public static readonly OperationResult ServiceUnavailable = new(InternalError.ServiceUnavailable);
        public static readonly OperationResult GatewayTimeout = new(InternalError.GatewayTimeout);

        public static readonly OperationResult NetworkFailure = new(InternalError.NetworkFailure);

        public static readonly OperationResult Unknown = new(InternalError.Unknown);

        private readonly Func<string> getMessage;

        public int Code { get; set; }

        public string Message => getMessage();

        public object? Reference { get; set; }

        public bool IsSuccess => Code == SuccessCode;

        public OperationResult() : this(SuccessCode)
        {
        }

        public OperationResult(int code, string? description = null)
        {
            Code = code;
            getMessage = () =>
            {
                StringBuilder msg = new();
                if (InternalError.TryGet(code, out var val))
                {
                    msg.Append(val);
                }
                else
                {
                    msg.Append(InternalError.Unknown);
                }

                if (description != null)
                {
                    if (msg.Length > 0)
                    {
                        msg.Append(':');
                    }
                    msg.Append(description);
                }

                if (code != SuccessCode)
                {
                    msg.Append(" [0x");
                    msg.Append(code);
                    msg.Append(']');
                }

                return msg.ToString();
            };
        }

        public OperationResult(OperationResult result) : this(result.Code)
        {
            getMessage = () => result.Message;
            Reference = result.Reference;
        }

        public bool HasResult()
        {
            Type type = GetType();
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(OperationResult<>);
        }

        public T? GetResult<T>()
        {
            bool hasResult = HasResult();
            return hasResult && this is OperationResult<T> result ? result.Result : default;
        }
    }

    public class OperationResult<T> : OperationResult
    {
        public OperationResult() : base()
        {
        }

        public OperationResult(T result) : base()
        {
            Result = result;
        }

        public OperationResult(int code, string? description = null) : base(code, description)
        {
        }

        public OperationResult(int code, T result, string? description = null) : this(code, description)
        {
            Result = result;
        }

        public OperationResult(OperationResult result) : base(result)
        {
        }

        public T? Result { get; set; }
    }
}
