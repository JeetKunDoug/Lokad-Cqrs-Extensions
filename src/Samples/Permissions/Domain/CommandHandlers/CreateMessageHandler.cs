using System;

using Commands;

using Context;

using Domain.Events;

using Lokad.Cqrs;
using Lokad.Cqrs.Extensions.Permissions;

using Security;

namespace Domain.CommandHandlers
{
    public class CreateMessageHandler : Define.Handle<CreateMessage>
    {
        private readonly Func<MyMessageContext> contextFactory;
        private readonly IMessageSender sender;

        public CreateMessageHandler(Func<MyMessageContext> contextFactory, IMessageSender sender)
        {
            this.contextFactory = contextFactory;
            this.sender = sender;
        }

        #region Implementation of IConsume<in MyCommand>

        public void Consume(CreateMessage message)
        {
            var user = contextFactory().User;

            Authorize(user);
            
            user.TakeOwnershipOf<Message>(message.Id);
            user.Permission().ForEntity<Note>(message.Id).OnRootOperation().Allow();

            sender.SendOne(new MessageCreated
            {
                Id = message.Id,
                Message = message.Message,
                UtcCreated = DateTime.UtcNow
            });
        }

        #endregion

        private void Authorize(PermissionsUser user)
        {
            if(user.IsAnonymous())
            {
                throw new Exception("Anonymous users can not create messages.");
            }

        }

    }
}