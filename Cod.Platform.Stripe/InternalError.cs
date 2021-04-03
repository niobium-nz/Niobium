using System;
using System.Collections.Generic;
using System.Text;

namespace Cod.Platform
{
    internal class InternalError : Cod.InternalError
    {
        public const int PaymentError_Unknown = 6999;
        public const int PaymentError_IncorrectCVC = 6000;
        public const int PaymentError_ExpiredCard = 6001;
        public const int PaymentError_InsufficientFunds = 6002;
    }
}
