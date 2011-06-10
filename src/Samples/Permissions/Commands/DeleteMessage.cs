using System;

using Lokad.Cqrs;

using ProtoBuf;

namespace Commands
{
    [ProtoContract]
    public class DeleteMessage : Define.Command
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }
    }
}