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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using CommonDomain;
using CommonDomain.Persistence;

using EventStore;
using EventStore.Persistence;

using Lokad.Cqrs.Extensions.EventStore.Events;

namespace Lokad.Cqrs.Extensions.EventStore
{
    internal class SnapshottingProcess : IEngineProcess
    {
        private readonly TimeSpan checkInterval;
        private readonly ISystemObserver observer;
        private readonly IStoreEvents eventStore;
        private readonly Func<string, Type> aggregateTypeResolver;
        private readonly IRepository repository;
        private readonly int threshold;

        public SnapshottingProcess(IStoreEvents eventStore, 
            Func<string, Type> aggregateTypeResolver, 
            IRepository repository, 
            int threshold, 
            TimeSpan checkInterval,
            ISystemObserver observer)
        {
            this.eventStore = eventStore;
            this.aggregateTypeResolver = aggregateTypeResolver;
            this.repository = repository;
            this.checkInterval = checkInterval;
            this.observer = observer;
            this.threshold = threshold;
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
        }

        #endregion

        #region Implementation of IEngineProcess

        public void Initialize()
        {
        }

        public Task Start(CancellationToken token)
        {
            return Task.Factory.StartNew(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    CreateSnapshots(eventStore.GetStreamsToSnapshot(threshold).ToArray());

                    token.WaitHandle.WaitOne(checkInterval);
                }
            });
        }

    private void CreateSnapshots(IEnumerable<StreamHead> streams)
    {
        foreach (StreamHead head in streams)
        {
            //NOTE: This uses a patched version of EventStore that loads commit headers in OptimisticEventStream.PopulateStream()
            // <code>
            // this.identifiers.Add(commit.CommitId);
            // this.headers = this.headers.Union(commit.Headers).ToDictionary(k => k.Key, k => k.Value);
            // </code>
            var stream = eventStore.OpenStream(head.StreamId, int.MinValue, int.MaxValue);

            //NOTE: Nasty hack but it works.
            var aggregateType = stream.UncommittedHeaders.Where(p=>p.Key=="AggregateType").First();
            var type = aggregateTypeResolver(aggregateType.Value.ToString());

            MethodInfo methodInfo = typeof(IRepository).GetMethod("GetById");
            MethodInfo method = methodInfo.MakeGenericMethod(type);

            object o = method.Invoke(repository, new object[]{head.StreamId, head.HeadRevision});
            var aggregate = (IAggregate) o;
            
            IMemento memento = aggregate.GetSnapshot();

            var snapshot = new Snapshot(head.StreamId, head.HeadRevision, memento);

            eventStore.AddSnapshot(snapshot);

            observer.Notify(new SnapshotTaken(head.StreamId, head.HeadRevision));
        }
    }

        #endregion
    }
}