using System;

using CommonDomain;
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

        internal Message(Guid id, IMemento memento) : this(id)
        {
            if(memento is MessageMemento)
            {
                var snapshot = (MessageMemento) memento;
                created = snapshot.UtcCreated;
                lastModified = snapshot.UtcLastModified;
                message = snapshot.Message;
            }
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

        protected override IMemento GetSnapshot()
        {
            return new MessageMemento
            {
                Message = message,
                UtcCreated = created,
                UtcLastModified = lastModified
            };
        }
    }

    public class MessageMemento : IMemento
    {
        public string Message { get; set; }
        public DateTime UtcCreated { get; set; }
        public DateTime UtcLastModified { get; set; }

        #region Implementation of IMemento

        Guid IMemento.Id { get; set; }
        int IMemento.Version { get; set; }

        #endregion
    }
}
