namespace Cod.Channel
{
    public class DisplayProperty(
        string propertyName,
        string displayName,
        Func<object, object> getValue, 
        int? order, 
        int? group, 
        string description,
        PropertyControl? control,
        bool? isSubject)
    {
        public string PropertyName { get; } = propertyName;

        public string DisplayName { get; protected set; } = displayName;

        public int Order { get; protected set; } = order ?? 0;

        public int Group { get; protected set; } = group ?? 0;

        public string Description { get; protected set; } = description;

        public Func<object, object> GetValue { get; protected set; } = getValue;

        public bool IsSubject { get; protected set; } = isSubject ?? false;

        public PropertyControl Control { get; protected set; } = control ?? PropertyControl.Label;
    }
}
