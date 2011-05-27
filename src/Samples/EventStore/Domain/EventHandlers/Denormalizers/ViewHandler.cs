using System;
using System.Diagnostics;

using Commands;

using Domain.Events;

using Lokad.Cqrs;
using Lokad.Cqrs.Feature.AtomicStorage;

using Views;

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
        }

        #endregion
    }

}