namespace Niobium.Channel
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class EditModeAttribute(
        PropertyControl control = PropertyControl.TextField,
        bool isReadOnly = false,
        bool isRequired = false,
        CalendarKind calendar = CalendarKind.Default)
        : Attribute
    {
        public bool IsRequired { get; set; } = isRequired;

        public bool IsReadOnly { get; set; } = isReadOnly;

        public PropertyControl Control { get; set; } = control;

        public CalendarKind Calendar { get; set; } = calendar;
    }
}
