﻿#region License

// Copyright (c) 2011, 2012, EventDay Inc.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by the <organization>.
// 4. Neither the name of the <organization> nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY  EventDay Inc. "AS IS" AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
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

using ProtoBuf;

using Rhino.Security;

namespace Lokad.Cqrs.Extensions.Permissions
{
    [ProtoContract]
    [Serializable]
    public class PermissionsUser : IUser
    {
        public PermissionsUser()
        {
        }

        public PermissionsUser(string name, Guid id)
        {
// ReSharper disable DoNotCallOverridableMethodsInConstructor
            Name = name;
            Id = id;
            // ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        #region IUser Members

        public virtual SecurityInfo SecurityInfo
        {
            get { return new SecurityInfo(Name, Id); }
        }

        #endregion

        [ProtoMember(1)]
        public virtual string Name { get; set; }

        [ProtoMember(2)]
        public virtual Guid Id { get; set; }

        public virtual bool Equals(PermissionsUser other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Id.Equals(Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (PermissionsUser)) return false;
            return Equals((PermissionsUser) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static readonly PermissionsUser Anonymous = new PermissionsUser("Anonymous", Guid.Empty);
    }
}