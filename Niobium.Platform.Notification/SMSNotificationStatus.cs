namespace Niobium.Platform.Notification
{
    public enum SMSNotificationStatus : int
    {
        Success = 1,

        InvalidNumber = 2,

        Poweroff = 3,

        OverdueBalance = 4,

        Blacklist = 5,

        Censorship = 6,

        Rejected = 7,

        CountLimit = 8,

        OtherTerminalError = 97,

        OtherServiceProviderError = 98,

        UnknownError = 99,
    }
}
