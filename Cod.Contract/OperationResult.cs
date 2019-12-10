using System.Text;

namespace Cod.Contract
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

        public static OperationResult Create(int code)
        {
            var msg = new StringBuilder();
            if (InternalError.Messages.ContainsKey(code))
            {
                msg.Append(InternalError.Messages[code]);
            }
            else
            {
                msg.Append("未知错误");
            }
            msg.Append(" 错误代码:");
            msg.Append(code.ToString());
            return new OperationResult(code)
            {
                Message = msg.ToString()
            };
        }
    }

    public class OperationResult<T> : OperationResult where T : class
    {
        private OperationResult(int code) : base(code)
        {
        }

        private OperationResult(int code, T result) : this(code) => this.Result = result;

        public T Result { get; set; }

        public static OperationResult<T> Create(T result) => new OperationResult<T>(SuccessCode, result);

        public static OperationResult<T> Create(int code, object reference)
        {
            var msg = new StringBuilder();
            if (InternalError.Messages.ContainsKey(code))
            {
                msg.Append(InternalError.Messages[code]);
            }
            else
            {
                msg.Append("未知错误");
            }
            msg.Append(" 错误代码:");
            msg.Append(code.ToString());
            return new OperationResult<T>(code)
            {
                Message = msg.ToString(),
                Reference = reference
            };
        }
    }
}
