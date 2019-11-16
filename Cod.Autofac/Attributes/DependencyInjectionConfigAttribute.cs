using System;

namespace AzureFunctions.Autofac
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DependencyInjectionConfigAttribute : Attribute
    {
        public Type Config { get; }

        public DependencyInjectionConfigAttribute(Type config) => this.Config = config;
    }
}
