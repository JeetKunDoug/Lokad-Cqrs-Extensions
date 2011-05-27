using System;
using System.Collections.Generic;
using System.Linq;

using Lokad.Cqrs;

using ProtoBuf;

namespace Commands
{
    [ProtoContract]
    public class MyMessageContext
    {
        [ProtoMember(1)]
        public string OriginatingMachine { get; set; }

        [ProtoMember(2)]
        public DateTime CreatedOnUtc { get; set; }

        [ProtoMember(3)]
        public string EnvelopeId { get; set; }

        [ProtoMember(4)]
        public int MessageIndex { get; set; }

        [ProtoMember(5)]
        public string MappedType { get; set; }

        [ProtoMember(6)]
        public DateTime DeliveredOnUtc { get; set; }

        public void ApplyTo(IDictionary<string, object> dictionary)
        {
            dictionary.Add(ContextAttributes.ORIGINATING_MACHINE, OriginatingMachine);
        }

        public static MyMessageContext Factory(ImmutableEnvelope envelope, ImmutableMessage message)
        {
            ImmutableAttribute address =
                envelope.GetAllAttributes().Where(a => a.Key == ContextAttributes.ORIGINATING_MACHINE).FirstOrDefault();

            return new MyMessageContext
            {
                OriginatingMachine = address == null ? "" : address.Value,
                CreatedOnUtc = envelope.CreatedOnUtc,
                DeliveredOnUtc = envelope.DeliverOnUtc,
                EnvelopeId = envelope.EnvelopeId,
                MessageIndex = message.Index,
                MappedType = message.MappedType.AssemblyQualifiedName
            };
        }
    }
}