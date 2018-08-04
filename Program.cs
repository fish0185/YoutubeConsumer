using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Autofac;
using JustSaying;
using JustSaying.AwsTools;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using YoutubeDownloader.Messages;
using YoutubeDownloader.Services;

namespace YoutubeDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            var containerBuilder = new ContainerBuilder();
            var container = ConfigContainer(containerBuilder).Build();

            //container.Resolve<IMessagePublisher>().PublishAsync(new YoutubeDownloadCommand { Url = "https://google.com" }).Wait();
           
            manualResetEvent.WaitOne();
            Console.WriteLine("done");
        }

        private static ContainerBuilder ConfigContainer(ContainerBuilder containerBuilder)
        {
            var builder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json")
                                .AddEnvironmentVariables();
            var configuration = builder.Build();
            var esUrl = configuration["elasticsearch"];
            Log.Logger = new LoggerConfiguration()
                            .Enrich.FromLogContext()
                            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(esUrl) ){
                                    AutoRegisterTemplate = true,
                                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6
                            })
                            .WriteTo.Console()
                            .CreateLogger();
            CreateMeABus.DefaultClientFactory = () =>
                new DefaultAwsClientFactory();
            var loggerFactory = new LoggerFactory().AddSerilog();
            containerBuilder.RegisterInstance(loggerFactory).AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).WithParameter("factory", loggerFactory);
            containerBuilder.RegisterType<HandlerResolver>().AsImplementedInterfaces();
            containerBuilder.Register(
                cxt =>
                    {
                        var publisher = CreateMeABus.WithLogging(loggerFactory)
                            .InRegion(RegionEndpoint.APSoutheast2.SystemName)
                            .ConfigurePublisherWith(
                                c =>
                                    {
                                        c.PublishFailureReAttempts = 3;
                                        c.PublishFailureBackoffMilliseconds = 50;
                                    })
                            .WithSnsMessagePublisher<YoutubeDownloadCommand>()
                            .WithSqsTopicSubscriber().IntoQueue("YoutubeDownloadCommand")
                            .ConfigureSubscriptionWith(
                                c =>
                                {
                                    c.MaxAllowedMessagesInFlight = 10;
                                    c.RetryCountBeforeSendingToErrorQueue = 3;
                                })
                            .WithMessageHandler<YoutubeDownloadCommand>(cxt.Resolve<IHandlerResolver>());
                        publisher.StartListening();
                        return publisher;
                    }).AsImplementedInterfaces().SingleInstance().AutoActivate();
           
            containerBuilder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AsClosedTypesOf(typeof(IHandlerAsync<>));
            containerBuilder.RegisterInstance(new AmazonS3Client(FallbackCredentialsFactory.GetCredentials(), RegionEndpoint.APSoutheast2)).AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterType<S3Service>().AsImplementedInterfaces();
            containerBuilder.RegisterInstance(configuration).AsImplementedInterfaces();
            
            return containerBuilder;
        }
    }
}
