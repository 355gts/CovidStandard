using Autofac;
using Autofac.Integration.WebApi;
using Covid.Common.Mapper;
using Covid.Repository.Facades;
using Covid.Repository.Factories;
using System.Reflection;
using System.Web.Http;

namespace Covid.Api.App_Start
{
    public class AutofacWebapiConfig
    {
        public static IContainer Container;

        public static void Initialize(HttpConfiguration config, string connectionString)
        {
            Initialize(config, RegisterServices(new ContainerBuilder(), connectionString));
        }


        public static void Initialize(HttpConfiguration config, IContainer container)
        {
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private static IContainer RegisterServices(ContainerBuilder builder, string connectionString)
        {
            //Register your Web API controllers.  
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            // load the assembly containing the mappers
            var executingAssembly = Assembly.Load("Covid.Api");

            // use and configure Autofac with custom registrations
            builder.Register(ctx => { return new CovidDbContextFactory("CovidApiContext"); }).As<ICovidDbContextFactory>().SingleInstance();
            builder.RegisterType<CovidRepositoryFacade>().As<ICovidRepositoryFacade>().SingleInstance();
            builder.Register(ctx => { return new Mapper(executingAssembly); }).As<IMapper>().SingleInstance();

            //Set the dependency resolver to be Autofac.  
            Container = builder.Build();

            return Container;
        }
    }
}