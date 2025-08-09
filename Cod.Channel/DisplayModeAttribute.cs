namespace Cod.Channel
{
    // decoration attribute for display controls in view models

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DisplayModeAttribute(
        PropertyControl control = PropertyControl.Label,
        bool isSubject = false)
        : Attribute
    {
        public bool IsSubject { get; set; } = isSubject;

        public PropertyControl Control { get; set; } = control;
    }
}
