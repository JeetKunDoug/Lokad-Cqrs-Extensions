using System;
using System.Collections.Generic;

using Lokad.Cqrs;
using Lokad.Cqrs.Extensions.Permissions;

using ProtoBuf;

namespace Context
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

        [ProtoMember(7)]
        public Guid IssuingUserId { get; set; }

        [ProtoMember(8)]
        public string IssuingUserName { get; set; }

        public void ApplyTo(IDictionary<string, object> dictionary)
        {
            dictionary.Add(ContextAttributes.ORIGINATING_MACHINE, OriginatingMachine);
            dictionary.Add(ContextAttributes.ISSUING_USER_ID, IssuingUserId);
            dictionary.Add(ContextAttributes.ISSUING_USER_NAME, IssuingUserName);
        }

        public static MyMessageContext Factory(ImmutableEnvelope envelope, ImmutableMessage message)
        {
            return new MyMessageContext
            {
                OriginatingMachine = envelope.GetString(ContextAttributes.ORIGINATING_MACHINE),
                IssuingUserId = envelope.GetGuid(ContextAttributes.ISSUING_USER_ID),
                IssuingUserName = envelope.GetString(ContextAttributes.ISSUING_USER_NAME),
                CreatedOnUtc = envelope.CreatedOnUtc,
                DeliveredOnUtc = envelope.DeliverOnUtc,
                EnvelopeId = envelope.EnvelopeId,
                MessageIndex = message.Index,
                MappedType = message.MappedType.AssemblyQualifiedName
            };
        }

        public PermissionsUser User
        {
            get { return new PermissionsUser(IssuingUserName, IssuingUserId); }
        }

    }
}