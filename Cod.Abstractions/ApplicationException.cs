using System.Text;

namespace Cod
{
    public class ApplicationException(int errorCode, string? internalMessage = null, Exception? innerException = null) : Exception(internalMessage, innerException)
    {
        private const int SuccessCode = 0;
        private readonly Func<string> getMessage = () =>
            {
                StringBuilder msg = new();
                if (InternalError.TryGet(errorCode, out var val))
                {
                    msg.Append(val);
                }
                else
                {
                    if (internalMessage != null)
                    {
                        msg.Append(internalMessage);
                    }
                }

                if (errorCode != SuccessCode)
                {
                    msg.Append(" [0x");
                    msg.Append(errorCode);
                    msg.Append(']');
                }

                return msg.ToString();
            };

        public int ErrorCode { get; set; } = errorCode;

        public override string Message => getMessage();

        public object? Reference { get; set; }
    }
}
