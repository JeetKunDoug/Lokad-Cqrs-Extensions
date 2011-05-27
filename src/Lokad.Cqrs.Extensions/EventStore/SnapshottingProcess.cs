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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using EventStore;
using EventStore.Persistence;

namespace Lokad.Cqrs.Extensions.EventStore
{
    internal class SnapshottingProcess : IEngineProcess
    {
        private readonly TimeSpan checkInterval;
        private readonly IStoreEvents snapshots;
        private readonly int threshold;

        public SnapshottingProcess(IStoreEvents snapshots, int threshold, TimeSpan checkInterval)
        {
            this.snapshots = snapshots;
            this.checkInterval = checkInterval;
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
                    CreateSnapshots(snapshots.GetStreamsToSnapshot(threshold).ToArray());

                    token.WaitHandle.WaitOne(checkInterval);
                }
            });
        }

        private void CreateSnapshots(StreamHead[] streams)
        {
            if (streams.Length == 0)
                return;

            foreach (var s in streams)
            {
                var eventStream = snapshots.OpenStream(s.StreamId, s.HeadRevision, int.MaxValue);

                var snapshot = new Snapshot(s.StreamId, s.SnapshotRevision + 1, eventStream);

                snapshots.AddSnapshot(snapshot);
            }
        }

        #endregion
    }
}