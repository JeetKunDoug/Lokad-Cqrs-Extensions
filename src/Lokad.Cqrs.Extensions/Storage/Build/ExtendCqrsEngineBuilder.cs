using System;

using Lokad.Cqrs.Build.Engine;

namespace Lokad.Cqrs.Extensions.Storage.Build
{
    public static class ExtendCqrsEngineBuilder
    {
        public static void ConfigureTableStorage(this CqrsEngineBuilder builder, Action<StorageModule> config)
        {
            var module = new StorageModule();
            config(module);
            builder.Advanced.RegisterModule(module);
        }
    }
}