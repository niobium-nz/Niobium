namespace Cod
{
    public enum PaymentMethodKind : int
    {
        WechatJSAPI = 0,

        AlipayJSAPI = 1,

        WechatInApp = 2,

        Visa = 32,

        MasterCard = 64,

        AmericanExpress = 128,

        UnionPay = 256,

        JCB = 512,

        DinnersClub = 1024,

        Discover = 2048,
    }
}
