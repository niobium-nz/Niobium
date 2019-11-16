using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace AzureFunctions.Autofac
{
    public class InjectValueProvider : IValueProvider
    {
        private readonly object value;

        public InjectValueProvider(object value) => this.value = value;

        public Type Type => this.value.GetType();

        public Task<object> GetValueAsync() => Task.FromResult(this.value);

        public string ToInvokeString() => this.value.ToString();
    }
}
