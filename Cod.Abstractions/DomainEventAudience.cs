namespace Cod
{
    [Flags]
    public enum DomainEventAudience : int
    {
        None = 0,
        Internal = 1,
        External = 2,
    }
}
