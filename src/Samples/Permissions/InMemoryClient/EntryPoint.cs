using System;
using System.Globalization;
using System.IO;
using System.Threading;

using Autofac;

using Commands;

using Context;

using Domain.CommandHandlers;

using Lokad.Cqrs.Build;
using Lokad.Cqrs.Build.Engine;
using Lokad.Cqrs.Extensions;
using Lokad.Cqrs.Extensions.Permissions;
using Lokad.Cqrs.Extensions.Permissions.Build;
using Lokad.Cqrs.Feature.AtomicStorage;

using NHibernate.Dialect;
using NHibernate.Driver;

using Security;

using Views;

namespace InMemoryClient
{
    public class EntryPoint
    {
        private static readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        private static CqrsEngineBuilder GetBuilder()
        {
            var builder = new CqrsEngineBuilder();

            //NOTE: Core Lokad CQRS Initialization
            builder.UseProtoBufSerialization();

            builder.Domain(config =>
            {
                config.InAssemblyOf<CreateMessage>();
                config.InAssemblyOf<CreateMessageHandler>();
                config.ContextFactory(MyMessageContext.Factory);
            });

            builder.Memory(config =>
            {
                config.AddMemoryProcess(Queues.MESSAGES, d=>{});
                config.AddMemorySender(Queues.MESSAGES);
            });

            builder.Storage(config =>
            {
                var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                config.AtomicIsInFiles(folder, s =>
                {
                    s.NameForSingleton(type => DefaultAtomicStorageStrategyBuilder.CleanName(type.Name) + ".json");
                    s.NameForEntity((type, key) => (DefaultAtomicStorageStrategyBuilder.CleanName(type.Name) + "-" + Convert.ToString(key, CultureInfo.InvariantCulture).ToLowerInvariant()) + ".json");
                    s.WithAssemblyOf<MessageView>();
                    s.CustomStaticSerializer(new AtomicStorageSerializerWithJson());
                });
                config.StreamingIsInFiles(folder);
            });

            //NOTE: Permissions Initialization
            builder.Permissions(config =>
            {
                const string CONNECTION_STRING = @"Data Source=.\sqlexpress;Initial Catalog=lokad-cqrs-permissions;Integrated Security=True;Pooling=False";

                config.InitializeDatabase();

                config.InformationExtractors(info => info.EntitiesAreInAssemblyOf<Message>());
                config.Persistance<SqlClientDriver, MsSql2008Dialect>(CONNECTION_STRING);
                config.Operations<OperationProvider>();
                config.UserIs<PermissionsUser>();
            });

            return builder;
        }

        private static void StartProgram(CqrsEngineBuilder builder)
        {
            builder.Advanced.RegisterObserver(new LogFileObserver());

            builder.Advanced.ConfigureContainer(b => b.RegisterType<Program>()
                                                         .WithParameter(TypedParameter.From(tokenSource)));


            var engine = builder.Build();

            engine.Start(tokenSource.Token);

            engine.Resolve<Program>().Run();
        }

        private static void Main()
        {
            StartProgram(GetBuilder());
        }
    }
}