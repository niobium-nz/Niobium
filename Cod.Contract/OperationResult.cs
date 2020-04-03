using System.Text;

namespace Cod
{
    public class OperationResult
    {
        public const int SuccessCode = 0;

        public int Code { get; set; }

        public string Message { get; set; }

        public object Reference { get; set; }

        public bool IsSuccess => this.Code == SuccessCode;

        protected OperationResult(int code) => this.Code = code;

        public static OperationResult Create() => new OperationResult(SuccessCode);

        public static OperationResult Create(int code, string description = null)
        {
            var msg = new StringBuilder();
            if (InternalError.Messages.ContainsKey(code))
            {
                msg.Append(InternalError.Messages[code]);
            }
            else
            {
                msg.Append("Unknown Error");
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
            return new OperationResult(code)
            {
                Message = msg.ToString()
            };
        }
    }

    public class OperationResult<T> : OperationResult
    {
        protected OperationResult(int code) : base(code)
        {
        }

        public OperationResult(int code, T result) : this(code) => this.Result = result;

        public T Result { get; set; }

        public static OperationResult<T> Create(T result) => new OperationResult<T>(SuccessCode, result);

        public static OperationResult<T> Create(int code, object reference, string description = null)
        {
            var msg = new StringBuilder();
            if (InternalError.Messages.ContainsKey(code))
            {
                msg.Append(InternalError.Messages[code]);
            }
            else
            {
                msg.Append("Unknown Error");
            }
            if (description != null)
            {
                msg.Append(":");
                msg.Append(description);
            }
            msg.Append(" [0x");
            msg.Append(code.ToString());
            msg.Append("]");
            return new OperationResult<T>(code)
            {
                Message = msg.ToString(),
                Reference = reference,
            };
        }
    }
}
