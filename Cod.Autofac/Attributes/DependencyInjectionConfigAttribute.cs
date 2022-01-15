namespace AzureFunctions.Autofac
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DependencyInjectionConfigAttribute : Attribute
    {
        public Type Config { get; }

        public DependencyInjectionConfigAttribute(Type config) => this.Config = config;
    }
}
