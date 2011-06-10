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
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace Lokad.Cqrs.Extensions.Permissions.Membership
{
    public class EventDayMembershipProvider : SqlMembershipProvider
    {
        public override MembershipUser CreateUser(string username, string password, string email,
                                                  string passwordQuestion, string passwordAnswer, bool isApproved,
                                                  object providerUserKey, out MembershipCreateStatus status)
        {
            var permissionWriter = ServiceLocator.Current.GetInstance<IPermissionWriter>();

            MembershipUser user = base.CreateUser(username, password, email, passwordQuestion, passwordAnswer,
                                                  isApproved, providerUserKey, out status);

            if (status != MembershipCreateStatus.Success)
            {
                return null;
            }

            var id = (Guid) user.ProviderUserKey;

            PermissionsUser newUser = permissionWriter.AddUser(new PermissionsUser
            {
                Name = user.UserName,
                Id = id
            });

            permissionWriter.Allow("/Event/Create", p=>p.For(newUser).OnEverything().DefaultLevel().Save());

            return user;
        }

        public static void PersistUserData(string username, bool remember)
        {
            var user = System.Web.Security.Membership.GetUser(username);

            using (var stream = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof (PermissionsUser));
                serializer.Serialize(stream, new PermissionsUser
                {
                    Name = username,
                    Id = (Guid)user.ProviderUserKey
                });

                byte[] bytes = stream.ToArray();

                string value = Encoding.ASCII.GetString(bytes);
                var ticket = new FormsAuthenticationTicket(1, username, DateTime.Now,
                                                           DateTime.Now.AddMinutes(30),
                                                           remember, value);
                var cookieValue = FormsAuthentication.Encrypt(ticket);
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, cookieValue);

                HttpContext.Current.Response.Cookies.Add(cookie);
            }
        }

        public static void RetrieveUserData()
        {
            var context = HttpContext.Current;
            if(context == null)
                return;

            HttpCookie authCookie = context.Request.Cookies[FormsAuthentication.FormsCookieName];

            if (authCookie != null)
            {
                FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);

                if(string.IsNullOrWhiteSpace(authTicket.UserData))
                    return;

                var bytes = Encoding.ASCII.GetBytes(authTicket.UserData);

                using(var stream = new MemoryStream(bytes))
                {
                    var serializer = new XmlSerializer(typeof(PermissionsUser));
                    var user = (PermissionsUser) serializer.Deserialize(stream);
                    var identity = new EventDayIdentity(user.Name, user.Id);
                    var principal = new EventDayPrincipal(identity);

                    context.User = Thread.CurrentPrincipal = principal;
                }
            }
        }
    }
}