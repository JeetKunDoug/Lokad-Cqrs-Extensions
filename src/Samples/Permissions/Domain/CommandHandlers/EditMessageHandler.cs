using System;

using Commands;

using Context;

using Domain.Events;

using Lokad.Cqrs;
using Lokad.Cqrs.Extensions.Permissions;
using Lokad.Cqrs.Extensions.Permissions.Specification;

using Security;

namespace Domain.CommandHandlers
{
    public class EditMessageHandler : Define.Handle<EditMessage>
    {
        private readonly Func<MyMessageContext> contextFactory;
        private readonly IMessageSender sender;

        public EditMessageHandler(Func<MyMessageContext> contextFactory, IMessageSender sender)
        {
            this.contextFactory = contextFactory;
            this.sender = sender;
        }

        #region Implementation of IConsume<in MyCommand>

        public void Consume(EditMessage command)
        {
            var context = contextFactory();
            
            Authorize(command.Id, context.User);

            sender.SendOne(new MessageEdited
            {
                Id = command.Id,
                Message = command.Message,
                OldMessage = command.OldMessage,
                UtcEdited = DateTime.UtcNow
            });
        }

        private static void Authorize(Guid id, PermissionsUser user)
        {
            var specification = user.Edit<Message>(id);

            if (specification.IsDenied())
            {
                throw new Exception(specification.AuthorizationInformation);
            }
        }

        #endregion
    }

    public class AddNoteHandler : Define.Handle<AddNote>
    {
        private readonly Func<MyMessageContext> contextFactory;
        private readonly IMessageSender sender;

        public AddNoteHandler(Func<MyMessageContext> contextFactory, IMessageSender sender)
        {
            this.contextFactory = contextFactory;
            this.sender = sender;
        }

        #region Implementation of IConsume<in MyCommand>

        public void Consume(AddNote command)
        {
            var context = contextFactory();

            Authorize(command.MessageId, context.User);

            sender.SendOne(new NoteAdded
            {
                MessageId = command.MessageId,
                Note = command.Note,
                UtcDateAdded = DateTime.UtcNow
            });
        }

        private static void Authorize(Guid id, PermissionsUser user)
        {
            IAuthorizationSpecification<Note> specification = user.Spec<Note>(id).BuildFor("add");

            if (specification.IsDenied())
            {
                throw new Exception(specification.AuthorizationInformation);
            }
        }

        #endregion
    }

}