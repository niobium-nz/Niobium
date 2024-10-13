using System.Text;

namespace Cod
{
    public class ApplicationException : Exception
    {
        private const int SuccessCode = 0;
        private readonly Func<string> getMessage;

        public int ErrorCode { get; set; }

        public override string Message => getMessage();

        public object Reference { get; set; }

        public ApplicationException(int errorCode, string internalMessage = null, Exception innerException = null)
            : base(internalMessage, innerException)
        {
            ErrorCode = errorCode;

            getMessage = () =>
            {
                StringBuilder msg = new();
                if (InternalError.TryGet(errorCode, out string val))
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
                    msg.Append(errorCode.ToString());
                    msg.Append("]");
                }

                return msg.ToString();
            };
        }
    }
}
