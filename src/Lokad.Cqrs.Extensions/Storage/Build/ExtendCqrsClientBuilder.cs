using System;

using Lokad.Cqrs.Build.Client;

namespace Lokad.Cqrs.Extensions.Storage.Build
{
    public static class ExtendCqrsClientBuilder
    {
        public static void ConfigureTableStorage(this CqrsClientBuilder builder, Action<StorageModule> config)
        {
            var module = new StorageModule();
            config(module);
            builder.Advanced.RegisterModule(module);
        }
    }
}