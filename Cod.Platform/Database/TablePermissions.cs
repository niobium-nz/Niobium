namespace Cod.Platform.Database
{
    [Flags]
    public enum TablePermissions
    {
        None = 0,
        Query = 1,
        Add = 2,
        Update = 4,
        Delete = 8
    }
}
