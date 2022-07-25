using Autofac;

namespace Cod.Platform.Autofac
{
    internal sealed class LifetimeScopeWrapper : IDisposable
    {
        public ILifetimeScope Scope { get; }

        public LifetimeScopeWrapper(IContainer container) => this.Scope = container.BeginLifetimeScope();

        public void Dispose() => this.Scope.Dispose();
    }
}
