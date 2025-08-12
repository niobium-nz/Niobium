using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Cod.Channel
{
    public interface IViewModelReflectionRegistration
    {
        Assembly? GetViewModelAssembly();
    }

    public class ReflectionCacheBootstrapper : IBootstrapper
    {
        public static ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache { get; } = new();
        public static ConcurrentDictionary<Type, MethodInfo[]> MethodCache { get; } = new();
        public static ConcurrentDictionary<Type, DisplayAttribute?> TypeDisplayCache { get; } = new();

        private readonly IEnumerable<IViewModelReflectionRegistration> registrations;

        public ReflectionCacheBootstrapper(IEnumerable<IViewModelReflectionRegistration> registrations)
        {
            this.registrations = registrations;
        }

        public Task InitializeAsync()
        {
            foreach (IViewModelReflectionRegistration registration in registrations)
            {
                Assembly? assembly = registration.GetViewModelAssembly();
                if (assembly == null)
                {
                    continue;
                }

                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(IViewModel).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                    {
                        PropertyCache[type] = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        MethodCache[type] = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                        TypeDisplayCache[type] = type.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
