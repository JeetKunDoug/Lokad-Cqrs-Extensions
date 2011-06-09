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

using Autofac;
using Autofac.Core;

using Commands;

using Domain.Events;

using EventStore;

using Lokad.Cqrs;
using Lokad.Cqrs.Feature.AtomicStorage;

using Shared;

using Views;

namespace Client
{
    internal class Program
    {
        private readonly IMessageSender sender;
        private readonly IStoreEvents store;
        private readonly NuclearStorage storage;
        private readonly ConsoleReader reader;

        public Program(IMessageSender sender, IStoreEvents store, NuclearStorage storage)
        {
            this.sender = new SenderDecorator(sender);
            this.store = store;
            this.storage = storage;
            reader = new ConsoleReader();
        }

        private void CreateMessage()
        {
            Guid id = Guid.NewGuid();
            string message = reader.GetString("Enter message");

            sender.SendOne(new CreateMessage
            {
                Id = id,
                Message = message
            } );
        }

        private void EditMessage()
        {
            var index = storage.GetSingletonOrNew<MessageIndex>();

            var pair = reader.GetValueOf("Pick message", index.Messages, p=>p.Value);

            Console.Out.WriteLine("Original Message:");
            Console.Out.WriteLine(pair.Value);
            Console.Out.WriteLine("-----");
            string message = reader.GetString("Enter new message");

            sender.SendOne(new EditMessage
            {
                Id = pair.Key,
                Message = message
            });
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
            Console.Out.WriteLine("Press any key after worker role is booted.");
            Console.ReadKey(true);

            do
            {
                Console.Clear();
                Choose()();

            } while (reader.Confirm("Run more operations?"));
        }

        private Action Choose()
        {
            var choice = reader.GetValueOf("Choose operation", new[]
            {
                new Tuple<string, Action>("Send message", CreateMessage),
                new Tuple<string, Action>("Edit message", EditMessage), 
                new Tuple<string, Action>("Replay all events", ReplayAllEvents),
                new Tuple<string, Action>("Replay events for message", ReplayMessageEvents)

            }, t => t.Item1);

            return choice.Item2;
        }
    }
}