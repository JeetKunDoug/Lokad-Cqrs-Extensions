using System;

using Lokad.Cqrs;

using ProtoBuf;

namespace Commands
{
    [ProtoContract]
    public class EditMessage : Define.Command
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public string Message { get; set; }

        [ProtoMember(3)]
        public string OldMessage { get; set; }
    }
}