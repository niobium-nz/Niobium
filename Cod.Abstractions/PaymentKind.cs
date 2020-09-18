using System;

namespace Cod
{
    [Flags]
    public enum PaymentKinds : int
    {
        Unknown = 0,

        Wechat = 1,

        Alipay = 2,

        CreditCard = 4,
    }
}
