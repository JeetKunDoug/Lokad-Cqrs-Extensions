using System;

using Lokad.Cqrs;

using ProtoBuf;

namespace Domain.Events
{
    [ProtoContract]
    public class MessageEdited : Define.Event
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public DateTime UtcEdited { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        [ProtoMember(4)]
        public string OldMessage { get; set; }
    }
}