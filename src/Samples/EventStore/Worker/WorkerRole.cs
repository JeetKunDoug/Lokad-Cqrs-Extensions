#region Copyright (c) 2011, EventDay Inc.

// Copyright (c) 2011, EventDay Inc.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the EventDay Inc. nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL EventDay Inc. BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Net;

using Commands;

using Domain.CommandHandlers;

using Lokad.Cqrs;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Build.Engine;
using Lokad.Cqrs.Extensions.EventStore;
using Lokad.Cqrs.Extensions.EventStore.Build;
using Lokad.Cqrs.Feature.AtomicStorage;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

using Views;

namespace Worker
{
    internal class WorkerRole : CqrsEngineRole
    {
        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 12;
            return base.OnStart();
        }

        private static IAzureStorageConfig CreateCloudStorageConfig()
        {
            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString");
            CloudStorageAccount.SetConfigurationSettingPublisher(
                (configName, configSetter) => configSetter(RoleEnvironment.GetConfigurationSettingValue(configName)));
            return AzureStorage.CreateConfig(CloudStorageAccount.FromConfigurationSetting("StorageConnectionString"));
        }

        #region Overrides of CqrsEngineRole

        protected override CqrsEngineHost BuildHost()
        {
            var builder = new CqrsEngineBuilder();

            builder.UseProtoBufSerialization();

            builder.Domain(config =>
            {
                config.InAssemblyOf<CreateMessage>();
                config.InAssemblyOf<CreateMessageHandler>();
                config.ContextFactory(MyMessageContext.Factory);
            });

            var storageConfig = CreateCloudStorageConfig();

            builder.Azure(config =>
            {

                config.AddAzureProcess(storageConfig, "eventstore-sample-messages");
                config.AddAzureSender(storageConfig, "eventstore-sample-messages");
            });

            builder.Storage(config => config.AtomicIsInAzure(storageConfig, s =>
            {
                s.WithAssemblyOf<MessageView>();
                s.CustomStaticSerializer(new AtomicStorageSerializerWithProtoBuf());
            }));

            builder.ConfigureJonathanOliverEventStore(config =>
            {
                config.ConnectionStringSettingName("EventStoreConnectionString");
                config.Hooks(p => p.Add(EventStorePipelineHook.Trace));
                config.Snapshots(s =>
                {
                    s.Enable();
                    s.CheckEvery(TimeSpan.FromSeconds(30));
                    //set this to something reasonable like 250. It's so low here to demonstrate background out "out-of-band" snapshotting.
                    s.MaxThreshold(2);
                });
            });

            return builder.Build();
        }

        #endregion
    }
}