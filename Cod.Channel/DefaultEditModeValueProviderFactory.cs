using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel
{
    internal class DefaultEditModeValueProviderFactory(IServiceProvider serviceProvider) : IEditModeValueProviderFactory
    {
        public IEnumerable<IEditModeValueProvider> Create(Type viewModelType)
        {
            var targetType = typeof(IEditModeValueProvider<>).MakeGenericType(viewModelType);
            return serviceProvider.GetServices(targetType).Cast<IEditModeValueProvider>();
        }
    }
}