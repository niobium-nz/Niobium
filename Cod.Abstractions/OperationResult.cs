using System;
using System.Text;

namespace Cod
{
    public class OperationResult
    {
        public const int SuccessCode = 0;
        public static readonly OperationResult Success = new OperationResult();
        public static readonly OperationResult BadRequest = new OperationResult(InternalError.BadRequest);
        public static readonly OperationResult AuthenticationRequired = new OperationResult(InternalError.AuthenticationRequired);
        public static readonly OperationResult PaymentRequired = new OperationResult(InternalError.PaymentRequired);
        public static readonly OperationResult Forbidden = new OperationResult(InternalError.Forbidden);
        public static readonly OperationResult NotFound = new OperationResult(InternalError.NotFound);
        public static readonly OperationResult NotAllowed = new OperationResult(InternalError.NotAllowed);
        public static readonly OperationResult NotAcceptable = new OperationResult(InternalError.NotAcceptable);
        public static readonly OperationResult RequestTimeout = new OperationResult(InternalError.RequestTimeout);
        public static readonly OperationResult Conflict = new OperationResult(InternalError.Conflict);
        public static readonly OperationResult PreconditionFailed = new OperationResult(InternalError.PreconditionFailed);
        public static readonly OperationResult Locked = new OperationResult(InternalError.Locked);
        public static readonly OperationResult UpgradeRequired = new OperationResult(InternalError.UpgradeRequired);
        public static readonly OperationResult TooManyRequests = new OperationResult(InternalError.TooManyRequests);

        public static readonly OperationResult InternalServerError = new OperationResult(InternalError.InternalServerError);
        public static readonly OperationResult ServiceUnavailable = new OperationResult(InternalError.ServiceUnavailable);
        public static readonly OperationResult GatewayTimeout = new OperationResult(InternalError.GatewayTimeout);

        public static readonly OperationResult NetworkFailure = new OperationResult(InternalError.NetworkFailure);

        public static readonly OperationResult Unknown = new OperationResult(InternalError.Unknown);

        private readonly Func<string> getMessage;

        public int Code { get; set; }

        public string Message => this.getMessage();

        public object Reference { get; set; }

        public bool IsSuccess => this.Code == SuccessCode;

        public OperationResult() : this(SuccessCode)
        {
        }

        public OperationResult(int code, string description = null)
        {
            this.Code = code;
            this.getMessage = () =>
            {
                var msg = new StringBuilder();
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
                        msg.Append(":");
                    }
                    msg.Append(description);
                }

                if (code != SuccessCode)
                {
                    msg.Append(" [0x");
                    msg.Append(code.ToString());
                    msg.Append("]");
                }

                return msg.ToString();
            }; 
        }

        public OperationResult(OperationResult result) : this(result.Code)
        {
            this.getMessage = () => result.Message;
            this.Reference = result.Reference;
        }
    }

    public class OperationResult<T> : OperationResult
    {
        public OperationResult() : base()
        {
        }

        public OperationResult(T result) : this() => this.Result = result;

        public OperationResult(int code, string description = null) : base(code, description)
        {
        }

        public OperationResult(int code, T result, string description = null) : this(code, description) => this.Result = result;

        public OperationResult(OperationResult result) : base(result)
        {
        }

        public T Result { get; set; }
    }
}
