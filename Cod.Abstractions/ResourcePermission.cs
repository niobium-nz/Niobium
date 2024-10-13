namespace Cod
{
    public class ResourcePermission : Permission
    {
        public ResourceType Type { get; set; }

        public string Resource { get; set; }
    }
}
