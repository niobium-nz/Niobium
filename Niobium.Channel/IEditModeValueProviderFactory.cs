namespace Niobium.Channel
{
    public interface IEditModeValueProviderFactory
    {
        IEnumerable<IEditModeValueProvider> Create(Type viewModelType);
    }
}