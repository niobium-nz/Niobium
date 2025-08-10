namespace Cod.Channel
{

    public interface IEditModeValueProvider<TViewModel> : IEditModeValueProvider
        where TViewModel : class
    {
        object? GetValue(TViewModel viewModel, string field, string setting);
    }

    public interface IEditModeValueProvider
    {
        object? GetValue(object viewModel, string field, string setting);
    }
}