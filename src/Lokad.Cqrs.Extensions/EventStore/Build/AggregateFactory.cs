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
using System.Reflection;

using CommonDomain;
using CommonDomain.Persistence;

namespace Lokad.Cqrs.Extensions.EventStore.Build
{
    /// <summary>
    ///   Factory for creating aggregates from with their Id using a private constructor that accepts
    ///   only one paramenter, the id of the aggregate to create.
    ///   This factory is used by the event store to create an aggregate prior to replaying it's events.
    /// </summary>
    public class AggregateFactory : IConstructAggregates
    {
        public static readonly IConstructAggregates Default = new AggregateFactory();

        private AggregateFactory()
        {
        }

        #region IConstructAggregates Members

        public IAggregate Build(Type type, Guid id, IMemento snapshot)
        {
            const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;

            var types = snapshot == null ? new[] { typeof(Guid) } : new[] { typeof(Guid), typeof(IMemento) };
            var args = snapshot == null ? new object[] { id } : new object[] { id, snapshot };

            ConstructorInfo constructor = type.GetConstructor(FLAGS, null, types, null);

            return constructor.Invoke(args) as IAggregate;
        }

        #endregion
    }
}