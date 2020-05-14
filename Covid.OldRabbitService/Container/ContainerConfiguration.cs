using Autofac;
using CommonUtils.Certificates;
using CommonUtils.Serializer;
using CommonUtils.Validation;
using Covid.Common.HttpClientHelper;
using Covid.Common.HttpClientHelper.Config;
using Covid.Common.HttpClientHelper.Factories;
using Covid.Common.Mapper;
using Covid.Message.Model.Publisher;
using Covid.Message.Model.Users;
using Covid.UserService.EventListeners;
using Covid.UserService.Processors;
using RabbitMQ.Client;
using RabbitMQWrapper;
using RabbitMQWrapper.Configuration;
using RabbitMQWrapper.Extensions;
using RabbitMQWrapper.Interfaces;
using System;
using System.Configuration;
using System.Reflection;
using System.Threading;

namespace Covid.UserService.Container
{
    public static class ContainerConfiguration
    {
        public static IContainer Configure(
            CancellationTokenSource eventListenerCancellationTokenSource,
            CancellationTokenSource cancellationTokenSource)
        {

            if (eventListenerCancellationTokenSource == null)
                throw new ArgumentNullException(nameof(eventListenerCancellationTokenSource));

            if (cancellationTokenSource == null)
                throw new ArgumentNullException(nameof(cancellationTokenSource));

            var containerBuilder = new ContainerBuilder();

            // register configuration sections
            var queueConfig = ConfigurationManager.GetSection("queueWrapper") as QueueWrapperConfiguration;
            var serviceConfig = ConfigurationManager.GetSection("restClients") as RestClientConfiguration;
            containerBuilder.Register(ctx => { return queueConfig; }).As<IQueueWrapperConfiguration>().SingleInstance();
            containerBuilder.Register(ctx => { return serviceConfig; }).As<IRestClientConfiguration>().SingleInstance();

            // load the assembly containing the mappers
            var executingAssembly = Assembly.Load("Covid.OldRabbitService");

            // register types
            containerBuilder.RegisterType<HttpClientHelper>().As<IHttpClientHelper>().WithParameter("serviceName", "covid").SingleInstance();
            containerBuilder.RegisterType<HttpClientFactory>().As<IHttpClientFactory>().SingleInstance();
            containerBuilder.RegisterType<CovidApiHelper>().As<ICovidApiHelper>().SingleInstance();
            containerBuilder.RegisterType<JsonSerializer>().As<ISerializer>().SingleInstance();
            containerBuilder.RegisterType<JsonSerializer>().As<IJsonSerializer>().SingleInstance();
            containerBuilder.RegisterType<UserProcessor>().As<IUserProcessor>().SingleInstance();
            //containerBuilder.RegisterType<QueueConnectionFactory>().As<IQueueConnectionFactory>().SingleInstance();
            containerBuilder.RegisterType<ValidationHelper>().As<IValidationHelper>().SingleInstance();
            containerBuilder.RegisterType<CertificateHelper>().As<ICertificateHelper>().SingleInstance();
            containerBuilder.RegisterType<ConnectionHandler>().As<IConnectionHandler>()
                .WithParameter("connectionFactory", new ConnectionFactory())
                .WithParameter("cancellationToken", eventListenerCancellationTokenSource.Token)
                .SingleInstance();
            containerBuilder.RegisterType<MessagePublisher>().As<IMessagePublisher>().SingleInstance();
            containerBuilder.Register(ctx => { return new Mapper(executingAssembly); }).As<IMapper>().SingleInstance();
            containerBuilder.RegisterType<UserEventListener>();

            containerBuilder.RegisterQueueConsumer<CreateUser>("NewUserQueueConsumer", cancellationTokenSource.Token);
            containerBuilder.RegisterQueuePublisher<User>("NewUserQueuePublisher", cancellationTokenSource.Token);

            return containerBuilder.Build();
        }
    }
}
