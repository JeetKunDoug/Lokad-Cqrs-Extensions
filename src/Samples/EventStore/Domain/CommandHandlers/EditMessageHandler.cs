using System;

using Commands;

using CommonDomain.Persistence;

using Lokad.Cqrs;
using Lokad.Cqrs.Extensions.EventStore;

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
            var entity = repository.GetById<Message>(message.Id, int.MaxValue);

            entity.ChangeMessage(message.Message);

            var context = contextFactory();

            //Save the context values into the eventstream's headers.
            repository.Save(entity, context.ApplyTo);
        }

        #endregion
    }
}