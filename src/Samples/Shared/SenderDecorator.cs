#region Copyright (c) 2011, EventDay Inc.

// Copyright (c) 2011, EventDay Inc.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the EventDay Inc. nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL EventDay Inc. BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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
            PutLocalIP(builder);
        }

        private void PutLocalIP(EnvelopeBuilder builder)
        {
            var ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
                .FirstOrDefault();

            builder.AddString(ContextAttributes.ORIGINATING_MACHINE, ip == null ? "?" : ip.ToString());
        }
    }
}