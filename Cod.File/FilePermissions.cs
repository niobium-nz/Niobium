namespace Cod.File
{
    [Flags]
    public enum FilePermissions
    {
        None = 0,
        Read = 1,
        Write = 2,
        Delete = 4,
        List = 8,
        Add = 0x10,
        Create = 0x20
    }
}
