using System;

using Lokad.Cqrs;

using ProtoBuf;

namespace Domain.Events
{
    [ProtoContract]
    public class MessageCreated : Define.Event
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public DateTime UtcCreated { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }
    }
}