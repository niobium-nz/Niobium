namespace Niobium.Finance
{
    [Flags]
    public enum PaymentChannels : int
    {
        None = 0,

        Alipay = 1,

        Wechat = 2,

        Cards = 4,
    }
}
