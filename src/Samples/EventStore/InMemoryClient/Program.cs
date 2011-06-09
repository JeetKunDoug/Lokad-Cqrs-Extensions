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
using System.Data.SqlTypes;
using System.Threading;

using Commands;

using Domain.Events;

using EventStore;

using Lokad.Cqrs;
using Lokad.Cqrs.Feature.AtomicStorage;

using Shared;

using Views;

namespace InMemoryClient
{
    internal class Program
    {
        private readonly IMessageSender sender;
        private readonly IStoreEvents store;
        private readonly NuclearStorage storage;
        private readonly CancellationTokenSource token;
        private readonly ConsoleReader reader;

        public Program(IMessageSender sender, IStoreEvents store, NuclearStorage storage, CancellationTokenSource token)
        {
            this.sender = new SenderDecorator(sender);
            this.store = store;
            this.storage = storage;
            this.token = token;
            reader = new ConsoleReader();
        }

        private void CreateAndEditMessage()
        {
            Guid id = Guid.NewGuid();
            sender.SendOne(new CreateMessage
            {
                Id = id,
                Message = "v1"
            });

            int edits = reader.GetInt("number of edits");

            for (int i = 2; i < edits+2; i++)
            {
                sender.SendOne(new EditMessage
                {
                    Id = id,
                    Message = string.Format("v{0}", i)
                });
                Thread.Sleep(100);
            }
        }

        private void EditMessageNTimes()
        {
            var index = storage.GetSingletonOrNew<MessageIndex>();

            var pair = reader.GetValueOf("Pick message", index.Messages, p => p.Value);
            int edits = reader.GetInt("number of edits");
            var current = int.Parse(pair.Value.Remove(0,1));
            for (int i = 0; i < edits; i++)
            {
                sender.SendOne(new EditMessage
                {
                    Id = pair.Key,
                    Message = string.Format("v{0}", current++)
                });
                Thread.Sleep(100);
            }
        }

        private void ReplayMessageEvents()
        {
            var index = storage.GetSingletonOrNew<MessageIndex>();

            var pair = reader.GetValueOf("Pick message", index.Messages, p => p.Value);

            var stream = store.OpenStream(pair.Key, 0, int.MaxValue);

            EnumerateEvents(stream.CommittedEvents);
        }

        private void ReplayAllEvents()
        {
            DateTime dateTime = DateTime.Parse(SqlDateTime.MinValue.ToString());

            foreach (var commit in store.GetFrom(dateTime))
            {
                EnumerateEvents(commit.Events);
            }
        }

        private void EnumerateEvents(IEnumerable<EventMessage> messageCollection)
        {
            foreach (var message in messageCollection)
            {
                if (message.Body is MessageCreated)
                {
                    var @event = (MessageCreated)message.Body;
                    Console.Out.WriteLine("{0,-20}: [{1}]: {2}", "message created", @event.Id, @event.Message);
                }
                if (message.Body is MessageEdited)
                {
                    var @event = (MessageEdited)message.Body;
                    Console.Out.WriteLine("{0,-20}: [{1}]: {2} -> {3}", "message edited", @event.Id, @event.OldMessage, @event.Message);
                }
            }
        }

        public void Run()
        {
            do
            {
                Console.Clear();
                Choose()();

            } while (reader.Confirm("Run more operations?"));

            token.Cancel(false);

            if (!token.Token.WaitHandle.WaitOne(5000))
            {
                Console.WriteLine("Terminating");
            }
        }

        private Action Choose()
        {
            var choice = reader.GetValueOf("Choose operation", new[]
            {
                new Tuple<string, Action>("Create message and perform a number of edits on it", CreateAndEditMessage),
                new Tuple<string, Action>("Edit message a number of times", EditMessageNTimes),
                new Tuple<string, Action>("Replay all events", ReplayAllEvents),
                new Tuple<string, Action>("Replay events for message", ReplayMessageEvents)

            }, t => t.Item1);

            return choice.Item2;
        }
    }
}