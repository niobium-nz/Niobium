namespace Niobium.Channel
{
    public abstract class EditModeValueProvider<TViewModel> : IEditModeValueProvider<TViewModel>
        where TViewModel : class
    {
        public abstract object? GetValue(TViewModel viewModel, string field, string setting);

        object? IEditModeValueProvider.GetValue(object viewModel, string field, string setting)
        {
            return GetValue((TViewModel)viewModel, field, setting);
        }
    }
}