using System;

using CommonDomain.Core;

using Domain.Events;

using Lokad.Cqrs;

namespace Domain
{
    public class Message : AggregateBase<Define.Event>
    {
        private DateTime created;
        private DateTime lastModified;
        private string message;

        private Message(Guid id)
        {
            Id = id;
        }

        public void CreateMessage(string message)
        {
            RaiseEvent(new MessageCreated
            {
                Id = Id,
                Message = message,
                UtcCreated = DateTime.UtcNow
            });
        }

        public void ChangeMessage(string newMessage)
        {
            if (newMessage.Equals(message))
                return;

            RaiseEvent(new MessageEdited
            {
                Id = Id,
                Message = newMessage,
                UtcEdited = DateTime.UtcNow,
                OldMessage = message
            });
        }

        protected void Apply(MessageCreated e)
        {
            message = e.Message;
            created = e.UtcCreated;
        }

        protected void Apply(MessageEdited e)
        {
            message = e.Message;
            lastModified = e.UtcEdited;
        }
    }
}
