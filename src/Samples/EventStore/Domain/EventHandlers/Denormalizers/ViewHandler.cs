using System;
using System.Collections.Generic;
using System.Diagnostics;

using Context;

using Domain.Events;

using Lokad.Cqrs;
using Lokad.Cqrs.Feature.AtomicStorage;

using Views;

using System.Linq;

namespace Domain.EventHandlers.Denormalizers
{
    public class ViewHandler : Define.Subscribe<MessageCreated>, Define.Subscribe<MessageEdited>
    {
        private readonly NuclearStorage storage;
        private readonly Func<MyMessageContext> contextFactory;

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

            storage.AddEntity(message.Id, entity);

            storage.AddOrUpdateSingleton(() =>
            {
                var index = new MessageIndex();
                index.Messages.Add(message.Id, message.Message);
                return index;
            }, i => i.Messages.Add(message.Id, message.Message));
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

            storage.UpdateSingleton<MessageIndex>(i =>
            {
                i.Messages[message.Id] = message.Message;
            });
        }

        #endregion
    }

}