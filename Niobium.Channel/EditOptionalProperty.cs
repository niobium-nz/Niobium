namespace Niobium.Channel
{
    public class EditOptionalProperty(
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
        Func<object, IEnumerable<EditOption>> getOptions)
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
        public IEnumerable<EditOption> GetOptions(object viewModel)
        {
            return getOptions(viewModel);
        }
    }
}
