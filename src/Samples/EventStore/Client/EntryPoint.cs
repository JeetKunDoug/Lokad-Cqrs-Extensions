using System;
using System.Reflection;

using Autofac;
using Autofac.Core;

using Context;

using Domain.Events;

using EventStore;

using Lokad.Cqrs;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Build.Client;

using Microsoft.WindowsAzure;

using Views;

namespace Client
{
    public class EntryPoint
    {
        private static void Main()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            IAzureStorageConfig storageConfig = AzureStorage.CreateConfig(account);

            var builder = new CqrsClientBuilder();

            builder.UseProtoBufSerialization();
            builder.Domain(m => m.InAssemblyOf<MessageCreated>());

            builder.Azure(config => config.AddAzureSender(storageConfig, Queues.MESSAGES));

            builder.Storage(config => config.AtomicIsInAzure(storageConfig, s => s.WithAssemblyOf<MessageView>()));

            builder.Advanced.ConfigureContainer(b =>
            {
                b.RegisterType<Program>().As<Program>();
                b.RegisterInstance(storageConfig).As<IAzureStorageConfig>();

                //This is solely to show the replaying of events. You wouldn't ordinarily need the eventstore in a cqrs client;
                b.RegisterInstance(ConfigureEventStore()).SingleInstance().As<IStoreEvents>();
            });

            CqrsClient cqrsClient = builder.Build();

            ILifetimeScope scope = cqrsClient.Scope;
            scope.Resolve<Program>().Run();
        }

        private static IStoreEvents ConfigureEventStore()
        {
            const string CONNECTION_NAME = @"EventStore";

            var store = Wireup.Init()
                .UsingSqlPersistence(CONNECTION_NAME)
                .InitializeStorageEngine()
                .UsingJsonSerialization()
                .UsingAsynchronousDispatcher()
                .Build();

            return store;
        }
    }
}