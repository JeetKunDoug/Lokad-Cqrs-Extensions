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
using System.Diagnostics;

using Context;

using Domain.Events;

using Lokad.Cqrs;
using Lokad.Cqrs.Feature.AtomicStorage;

using Views;

namespace Domain.EventHandlers.Denormalizers
{
    public class ViewHandler : Define.Subscribe<MessageCreated>, Define.Subscribe<MessageEdited>
    {
        private readonly Func<MyMessageContext> contextFactory;
        private readonly NuclearStorage storage;

        public ViewHandler(NuclearStorage storage, Func<MyMessageContext> contextFactory)
        {
            this.storage = storage;
            this.contextFactory = contextFactory;
        }

        #region Implementation of IConsume<in AggregateCreated>

        public void Consume(MessageCreated message)
        {
            Trace.TraceInformation("DENORMALIZING MESSAGE CREATED EVENT {0}[{1}]...", message.Message, message.Id);

            MyMessageContext context = contextFactory();

            var entity = new MessageView
            {
                Context = context,
                Message = message.Message,
                UtcCreated = message.UtcCreated
            };

            storage.AddOrUpdateEntity(message.Id, entity);

            storage.AddOrUpdateSingleton(() =>
            {
                var index = new MessageIndex();
                index.Messages.Add(message.Id, message.Message);
                return index;
            }, i => i.Messages[message.Id] = message.Message);
        }

        #endregion

        #region Implementation of IConsume<in MessageEdited>

        public void Consume(MessageEdited message)
        {
            Trace.TraceInformation("DENORMALIZING MESSAGE EDITED EVENT {0}[{1}]...", message.Message, message.Id);

            MyMessageContext context = contextFactory();

            storage.UpdateEntity<MessageView>(message.Id, v =>
            {
                v.Context = context;
                v.Message = message.Message;
                v.UtcLastModified = message.UtcEdited;
            });

            storage.UpdateSingleton<MessageIndex>(i => { i.Messages[message.Id] = message.Message; });
        }

        #endregion
    }
}