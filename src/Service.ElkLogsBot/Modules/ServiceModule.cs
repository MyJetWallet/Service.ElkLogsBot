using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Service.ElkLogsBot.Services;

namespace Service.ElkLogsBot.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ErrorLogHandler>().As<IStartable>().SingleInstance().AutoActivate();
        }
    }
}