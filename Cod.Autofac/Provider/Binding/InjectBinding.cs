using System;
using System.Threading.Tasks;
using AzureFunctions.Autofac.Configuration;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace AzureFunctions.Autofac
{
    internal class InjectBinding : IBinding
    {
        private readonly Type type;
        private readonly string name;
        private readonly string className;
        public bool FromAttribute => true;
        public InjectBinding(Type type, string name, string className)
        {
            this.type = type;
            this.name = name;
            this.className = className;
        }

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context) =>
            Task.FromResult((IValueProvider)new InjectValueProvider(value));

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            var value = DependencyInjection.Resolve(this.type, this.name, this.className, context.FunctionInstanceId);
            return Task.FromResult((IValueProvider)new InjectValueProvider(value));
        }

        public ParameterDescriptor ToParameterDescriptor() => new ParameterDescriptor();
    }
}