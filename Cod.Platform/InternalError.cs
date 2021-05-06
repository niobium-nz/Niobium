namespace Cod.Platform
{
    public class InternalError : Cod.InternalError
    {
        public const int PaymentErrorUnknown = 6999;
        public const int PaymentErrorIncorrectCVC = 6000;
        public const int PaymentErrorExpiredCard = 6001;
        public const int PaymentErrorInsufficientFunds = 6002;
    }
}
