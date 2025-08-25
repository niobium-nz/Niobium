namespace Niobium.Channel
{
    public class DisplayAction(
        string? displayName,
        Func<object, Task> action,
        int? order,
        int? group,
        string? description)
    {
        public string? DisplayName { get; protected set; } = displayName;

        public int Order { get; protected set; } = order ?? 0;

        public int Group { get; protected set; } = group ?? 0;

        public Func<object, Task> Action { get; protected set; } = action;

        public string? Description { get; protected set; } = description;
    }
}
