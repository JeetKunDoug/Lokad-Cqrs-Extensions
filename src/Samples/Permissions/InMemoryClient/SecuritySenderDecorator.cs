using System;
using System.Threading;

using Context;

using Lokad.Cqrs;
using Lokad.Cqrs.Core.Envelope;

using Security;

namespace InMemoryClient
{
    public class SecuritySenderDecorator : IMessageSender
    {
        private readonly IMessageSender sender;

        public SecuritySenderDecorator(IMessageSender sender)
        {
            this.sender = sender;
        }

        #region IMessageSender Members

        public void SendOne(object content)
        {
            SendOne(content, b => { });
        }

        public void SendOne(object content, Action<EnvelopeBuilder> configure)
        {
            sender.SendOne(content, b =>
            {
                DecorateEnvelope(b);
                configure(b);
            });
        }

        public void SendBatch(object[] content)
        {
            SendBatch(content, b => { });
        }

        public void SendBatch(object[] content, Action<EnvelopeBuilder> configure)
        {
            sender.SendBatch(content, b =>
            {
                DecorateEnvelope(b);
                configure(b);
            });
        }

        #endregion

        private void DecorateEnvelope(EnvelopeBuilder builder)
        {
            PutCurrentUser(builder);
        }

        private void PutCurrentUser(EnvelopeBuilder builder)
        {
            var principal = Thread.CurrentPrincipal as CustomPrincipal ??
                            new CustomPrincipal(new CustomIdentity("Anonymous", Guid.NewGuid()));

            var identity = principal.CustomIdentity;

            builder.AddString(ContextAttributes.ISSUING_USER_ID, identity.ID.ToString());
            builder.AddString(ContextAttributes.ISSUING_USER_NAME, identity.Name);
        }
    }
}