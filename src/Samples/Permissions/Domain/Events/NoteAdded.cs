using System;

using Lokad.Cqrs;

using ProtoBuf;

namespace Domain.Events
{
    [ProtoContract]
    public class NoteAdded : Define.Event
    {
        [ProtoMember(1)]
        public Guid MessageId { get; set; }

        [ProtoMember(2)]
        public string Note { get; set; }

        [ProtoMember(3)]
        public DateTime UtcDateAdded { get; set; }
    }
}