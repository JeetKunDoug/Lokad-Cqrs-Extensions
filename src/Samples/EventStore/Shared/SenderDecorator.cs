using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using Context;

using Lokad.Cqrs;
using Lokad.Cqrs.Core.Envelope;

namespace Shared
{
    public class SenderDecorator : IMessageSender
    {
        private readonly IMessageSender sender;

        public SenderDecorator(IMessageSender sender)
        {
            this.sender = sender;
        }

        public void SendOne(object content)
        {
            SendOne(content, b=> { });
        }

        public void SendOne(object content, Action<EnvelopeBuilder> configure)
        {
            sender.SendOne(content, b=>
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

        private void DecorateEnvelope(EnvelopeBuilder builder)
        {
            PutLocalIP(builder);
        }

        private void PutLocalIP(EnvelopeBuilder builder)
        {
            var ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
                .FirstOrDefault();

            builder.AddString(ContextAttributes.ORIGINATING_MACHINE, ip==null ? "?" : ip.ToString());
        }
    }
}