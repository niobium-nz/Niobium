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

        public ApplicationException(int errorCode, string description = null)
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
