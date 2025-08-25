using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Channel
{
    internal sealed class DefaultEditModeValueProviderFactory(IServiceProvider serviceProvider) : IEditModeValueProviderFactory
    {
        public IEnumerable<IEditModeValueProvider> Create(Type viewModelType)
        {
            Type targetType = typeof(IEditModeValueProvider<>).MakeGenericType(viewModelType);
            return serviceProvider.GetServices(targetType).Cast<IEditModeValueProvider>();
        }
    }
}