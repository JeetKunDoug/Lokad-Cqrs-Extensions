using System;

using Lokad.Cqrs;

using ProtoBuf;

namespace Commands
{
    [ProtoContract]
    public class AddNote : Define.Command
    {
        [ProtoMember(1)]
        public Guid MessageId { get; set; }

        [ProtoMember(2)]
        public string Note { get; set; }
    }
}