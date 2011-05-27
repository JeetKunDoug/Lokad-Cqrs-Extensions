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
using System.Collections.Generic;
using System.Linq;

using Autofac;
using Autofac.Core;

using CommonDomain;
using CommonDomain.Core;
using CommonDomain.Persistence;
using CommonDomain.Persistence.EventStore;

using EventStore;
using EventStore.Dispatcher;

using Lokad.Cqrs.Core.Envelope;

namespace Lokad.Cqrs.Extensions.EventStore.Build
{
    public class EventStoreModule : HideObjectMembersFromIntelliSense, IModule, IPersistanceConfiguration,
                                    IPipelineConfiguration, ISnapshottingConfiguration
    {
        private readonly ContainerBuilder builder = new ContainerBuilder();
        private IConstructAggregates aggregateConstructor = AggregateFactory.Default;
        private string eventStoreConnectionString = "EventStoreConnectionString";

        #region IPersistanceConfiguration Members

        public IPipelineConfiguration ConnectionStringSettingName(string connectionString)
        {
            eventStoreConnectionString = connectionString;
            return this;
        }

        #endregion

        public EventStoreModule ConstructAggregatesWith(IConstructAggregates constructor)
        {
            aggregateConstructor = constructor;
            return this;
        }

        void IModule.Configure(IComponentRegistry componentRegistry)
        {
            builder.Register(ComposeEventStore)
                .SingleInstance()
                .As<IStoreEvents, IAccessSnapshots>();

            builder.RegisterType<ConflictDetector>().As<IDetectConflicts>();
            builder.RegisterType<EventStoreRepository>().As<IRepository>();
            builder.RegisterInstance(aggregateConstructor).As<IConstructAggregates>();

            builder.Update(componentRegistry);
        }

        private IStoreEvents ComposeEventStore(IComponentContext context)
        {
            var scope = context.Resolve<ILifetimeScope>();

            var publisher = new DelegateMessagePublisher(commit => DispatchCommit(scope, commit));

            var pipelineHooks = context.Resolve<IEnumerable<IPipelineHook>>();

            var store = Wireup.Init()
                .UsingSqlAzurePersistence(eventStoreConnectionString)
                .InitializeStorageEngine()
                .UsingJsonSerialization()
                .HookIntoPipelineUsing(pipelineHooks)
                .UsingAsynchronousDispatcher()
                .PublishTo(publisher)
                .Build();

            return store;
        }

        private static void DispatchCommit(IComponentContext context, Commit commit)
        {
            var sender = context.Resolve<IMessageSender>();

            List<EventMessage> events = commit.Events;

            var messages = events.Select(e => e.Body).ToArray();

            sender.SendBatch(messages, b => AddHeadersToEnvelope(commit.Headers, b));
        }

        private static void AddHeadersToEnvelope(Dictionary<string, object> headers, EnvelopeBuilder b)
        {
            if (headers == null)
                return;

            foreach (var header in headers)
            {
                b.AddString(header.Key, header.Value.ToString());
            }
        }

        #region Implementation of IPipelineConfiguration

        public ISnapshottingConfiguration Hooks(Action<PipelineModule> config)
        {
            var module = new PipelineModule();
            config(module);
            builder.RegisterModule(module);
            return this;
        }

        #endregion

        #region Implementation of ISnapshottingConfiguration

        public void Snapshots(Action<SnapshotModule> config)
        {
            var module = new SnapshotModule();
            config(module);
            builder.RegisterModule(module);
        }

        #endregion
    }
}