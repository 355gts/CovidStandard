using Autofac;
using System.Configuration;

namespace Covid.Common.Extensions
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterConfigurationSection<TInterface, TType>(this ContainerBuilder containerBuilder, Configuration configuration, string sectionName) where TInterface : class where TType : class
        {
            var config = configuration.GetSection(sectionName) as TType;

            containerBuilder.Register(ctx => { return config; }).As<TInterface>().SingleInstance();

            return containerBuilder;
        }

        public static ContainerBuilder RegisterConfigurationSection<TType>(this ContainerBuilder containerBuilder, Configuration configuration, string sectionName) where TType : class
        {
            var config = configuration.GetSection(sectionName) as TType;

            containerBuilder.Register(ctx => { return config; }).SingleInstance();

            return containerBuilder;
        }
    }
}
