using Autofac;

namespace Cod.Channel.Mobile
{
    public class DependencyModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder) => builder.RegisterType<NavigatorAdaptor>().AsImplementedInterfaces().SingleInstance();
    }
}
