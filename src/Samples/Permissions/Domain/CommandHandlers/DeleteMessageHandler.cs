using System;

using Commands;

using Context;

using Domain.Events;

using Lokad.Cqrs;
using Lokad.Cqrs.Extensions.Permissions;

using Security;

namespace Domain.CommandHandlers
{
    public class DeleteMessageHandler : Define.Handle<DeleteMessage>
    {
        private readonly Func<MyMessageContext> contextFactory;
        private readonly IMessageSender sender;

        public DeleteMessageHandler(Func<MyMessageContext> contextFactory, IMessageSender sender)
        {
            this.contextFactory = contextFactory;
            this.sender = sender;
        }

        #region Implementation of IConsume<in MyCommand>

        public void Consume(DeleteMessage command)
        {
            var context = contextFactory();

            Authorize(command.Id, context.User);

            sender.SendOne(new MessageDeleted
            {
                Id = command.Id,
                UtcDeleted = DateTime.UtcNow
            });
        }

        private static void Authorize(Guid id, PermissionsUser user)
        {
            var specification = user.Delete<Message>(id);

            if (specification.IsDenied())
            {
                throw new Exception(specification.AuthorizationInformation);
            }
        }

        #endregion
    }
}