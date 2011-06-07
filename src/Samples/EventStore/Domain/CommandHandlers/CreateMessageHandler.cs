using System;

using Commands;

using CommonDomain.Persistence;

using Context;

using Lokad.Cqrs;

namespace Domain.CommandHandlers
{
    public class CreateMessageHandler : Define.Handle<CreateMessage>
    {
        private readonly Func<MyMessageContext> contextFactory;
        private readonly IRepository repository;

        public CreateMessageHandler(Func<MyMessageContext> contextFactory, IRepository repository)
        {
            this.contextFactory = contextFactory;
            this.repository = repository;
        }

        #region Implementation of IConsume<in MyCommand>

        public void Consume(CreateMessage message)
        {
            var context = contextFactory();

            var transaction = repository.BeginAggregateTransaction<Message>(message.Id, context.ApplyTo);

            using (transaction)
            {
                Message aggregate = transaction.Aggregate;

                aggregate.CreateMessage(message.Message);
            }
        }

        #endregion
    }
}