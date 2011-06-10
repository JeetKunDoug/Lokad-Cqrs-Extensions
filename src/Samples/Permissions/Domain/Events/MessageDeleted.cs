using System;

using Lokad.Cqrs;

using ProtoBuf;

namespace Domain.Events
{
    [ProtoContract]
    public class MessageDeleted : Define.Event
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public DateTime UtcDeleted { get; set; }
    }
}