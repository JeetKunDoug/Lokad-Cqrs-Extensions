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

using Commands;

using Domain.Events;

using EventStore;

using Lokad.Cqrs;

namespace Client
{
    internal class Program
    {
        private readonly IMessageSender sender;
        private readonly IStoreEvents store;
        private readonly ConsoleReader reader;
        private readonly Dictionary<Guid, string> messages;

        public Program(IMessageSender sender, IStoreEvents store)
        {
            this.sender = sender;
            this.store = store;
            reader = new ConsoleReader();
            messages = new Dictionary<Guid, string>();
        }

        private void SendCommand()
        {
            Guid id = Guid.NewGuid();
            string message = reader.GetString("Enter message");

            sender.SendOne(new CreateMessage
            {
                Id = id,
                Message = message
            });

            messages.Add(id, message);
        }

        private void EditMessage()
        {
            var pair = reader.GetValueOf("Pick message", messages, p=>p.Value);

            Console.Out.WriteLine("Original Message:");
            Console.Out.WriteLine(pair.Value);
            Console.Out.WriteLine("-----");
            string message = reader.GetString("Enter new message");

            sender.SendOne(new EditMessage
            {
                Id = pair.Key,
                Message = message
            });

            messages[pair.Key] = message;
        }

        private void ReplayAllEvents()
        {
            DateTime dateTime = DateTime.Parse(SqlDateTime.MinValue.ToString());
            var commits = store.GetFrom(dateTime);

            foreach (var commit in commits)
            {
                commit.Events.ForEach(e=>
                {
                    if(e.Body is MessageCreated)
                    {
                        var @event = (MessageCreated)e.Body;
                        Console.Out.WriteLine("Create message: [{0}]: {1}", @event.Id, @event.Message);
                    }
                    if(e.Body is EditMessage)
                    {
                        var @event = (EditMessage)e.Body;
                        Console.Out.WriteLine("Edit message: [{0}]: {1} -> {2}", @event.Id, @event.OldMessage, @event.Message);
                    }
                });
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
                new Tuple<string, Action>("Send message", SendCommand),
                new Tuple<string, Action>("Edit message", EditMessage), 
                new Tuple<string, Action>("Replay all events", ReplayAllEvents)

            }, t => t.Item1);

            return choice.Item2;
        }
    }
}