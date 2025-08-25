namespace Niobium.Identity
{
    public enum AuthenticationKind : int
    {
        Unknown = 0,

        Wechat = 10,

        Alipay = 20,

        Google = 40,

        Microsoft = 41,

        Facebook = 42,

        Instagram = 43,

        Twitter = 44,

        Apple = 50,

        Email = 60,

        SMS = 70,

        PhoneCall = 80,

        Username = 90,

        Authenticator = 99,
    }
}
