namespace Cod.Channel
{
    public class DisplayAction(
        string? displayName, 
        Func<object, Task> action, 
        int? order = 0, 
        string? description = null)
    {
        public string? DisplayName { get; set; } = displayName;

        public int Order { get; set; } = order ?? 0;

        public Func<object, Task> Action { get; set; } = action;

        public string? Description { get; set; } = description;
    }
}
