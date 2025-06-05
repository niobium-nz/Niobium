namespace Cod.Platform.Finance
{
    [Flags]
    public enum PaymentMethodKind : int
    {
        None = 0,

        AlipayJSAPI = 1,

        WechatInApp = 2,

        WechatJSAPI = 4,

        Visa = 32,

        MasterCard = 64,

        AmericanExpress = 128,

        UnionPay = 256,

        JCB = 512,

        DinnersClub = 1024,

        Discover = 2048,

        Afterpay = 4096,
    }
}
