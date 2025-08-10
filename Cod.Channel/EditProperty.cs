namespace Cod.Channel
{
    public class EditProperty : DisplayProperty
    {
        public EditProperty(
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
            bool? isRequired)
         : base(propertyName, displayName, getValue, order, group, description, control, false)
        {
            PropertyType = propertyType ?? throw new ArgumentNullException(nameof(propertyType));
            IsRequired = isRequired ?? false;
            IsReadOnly = isReadOnly ?? false;
            SetValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
            Control = control ?? PropertyControl.TextField;
        }

        public Type PropertyType { get; }

        public bool IsRequired { get; }

        public bool IsReadOnly { get; }

        public Action<object, object?> SetValue { get; }
    }
}
