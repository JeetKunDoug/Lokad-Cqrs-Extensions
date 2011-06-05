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
            var entity = repository.GetById<Message>(message.Id, int.MaxValue);

            entity.Create(message.Message);

            var context = contextFactory();

            //Save the context values into the eventstream's headers.
            repository.Save(entity, context.ApplyTo);
        }

        #endregion
    }
}