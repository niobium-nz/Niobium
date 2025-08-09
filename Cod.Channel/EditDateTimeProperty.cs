namespace Cod.Channel
{
    public class EditDateTimeProperty(
        string displayName,
        string propertyName,
        Type propertyType,
        Func<object, object> getValue,
        Action<object, object> setValue,
        int? order,
        int? group,
        string description,
        PropertyControl? control,
        bool? isReadOnly,
        bool? isRequired,
        CalendarKind? calendar)
        : EditProperty(
            displayName,
            propertyName,
            propertyType,
            getValue,
            setValue,
            order,
            group,
            description,
            control,
            isReadOnly,
            isRequired)
    {
        public CalendarKind Calendar { get; } = calendar ?? CalendarKind.Default;
    }
}
