namespace Niobium.Channel
{
    public class EditDateTimeProperty(
        string? displayName,
        string propertyName,
        Type propertyType,
        Func<object, object?> getValue,
        Action<object, object?> setValue,
        int? order,
        int? group,
        string? description,
        PropertyControl? control,
        bool? isReadOnly,
        bool? isRequired,
        Func<object, CalendarKind?> getCalendarKind)
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
        public CalendarKind GetCalendarKind(object viewModel)
        {
            return getCalendarKind(viewModel) ?? CalendarKind.Default;
        }
    }
}
