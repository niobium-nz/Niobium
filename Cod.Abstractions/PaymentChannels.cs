using System;

namespace Cod
{
    [Flags]
    public enum PaymentChannels : int
    {
        Wechat = 0,

        Alipay = 1,

        Cards = 2,
    }
}
