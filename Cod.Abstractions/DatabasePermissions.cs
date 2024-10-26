namespace Cod
{
    [Flags]
    public enum DatabasePermissions
    {
        None = 0,
        Query = 1,
        Add = 2,
        Update = 4,
        Delete = 8
    }
}
