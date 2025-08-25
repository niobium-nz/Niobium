namespace Niobium.Messaging
{
    [Flags]
    public enum MessagingPermissions
    {
        None = 0,
        Read = 1,
        Add = 2,
        Update = 4,
        ProcessMessages = 8
    }
}
