using System;
using System.Diagnostics;

using Commands;

using CommonDomain.Persistence;

using Context;

using Lokad.Cqrs;

namespace Domain.CommandHandlers
{
    public class EditMessageHandler : Define.Handle<EditMessage>
    {
        private readonly Func<MyMessageContext> contextFactory;
        private readonly IRepository repository;

        public EditMessageHandler(Func<MyMessageContext> contextFactory, IRepository repository)
        {
            this.contextFactory = contextFactory;
            this.repository = repository;
        }

        #region Implementation of IConsume<in MyCommand>

        public void Consume(EditMessage message)
        {
            var context = contextFactory();

            var transaction = repository.BeginAggregateTransaction<Message>(message.Id, context.ApplyTo);
            
            using(transaction)
            {
                Message entity = transaction.Aggregate;

                try
                {
                    entity.ChangeMessage(message.Message);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                    transaction.Rollback();
                }
            }
        }

        #endregion
    }
}