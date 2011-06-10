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
using System.Collections.Generic;
using System.Threading;

using Commands;

using Lokad.Cqrs;
using Lokad.Cqrs.Extensions.Permissions;
using Lokad.Cqrs.Feature.AtomicStorage;

using Security;

using Shared;

using Views;

namespace InMemoryClient
{
    internal class Program
    {
        private readonly IMessageSender sender;
        private readonly NuclearStorage storage;
        private readonly IPermissionSystem permissions;
        private readonly CancellationTokenSource token;
        private readonly ConsoleReader reader;
        private readonly IEnumerable<PermissionsUser> users;
        private readonly PermissionsUser admin;

        public Program(IMessageSender sender, NuclearStorage storage, IPermissionSystem permissions, CancellationTokenSource token)
        {
            this.sender = new SecuritySenderDecorator(new SenderDecorator(sender));
            this.storage = storage;
            this.permissions = permissions;
            this.token = token;

            reader = new ConsoleReader();

            admin = new PermissionsUser("Admin", Guid.NewGuid());

            users = new List<PermissionsUser>
            {
                admin,
                new PermissionsUser("Chris", Guid.NewGuid()),
                new PermissionsUser("Scott", Guid.NewGuid()),
                PermissionsUser.Anonymous
            };

            InitializePermissions();
        }

        private void InitializePermissions()
        {
            foreach (var user in users)
            {
                permissions.AddUser(user);
            }

            permissions.Allow("/message", p => p.For(admin).OnEverything().DefaultLevel().Save());
        }

        private void RunAs()
        {
            var user = reader.GetValueOf("Run as", users, u=>u.Name);

            var identity = new CustomIdentity(user.Name, user.Id);
            Thread.CurrentPrincipal = new CustomPrincipal(identity);
            Console.Clear();
            Console.Out.WriteLine("Running as: {0}", identity.Name);
        }

        private PermissionsUser GetCurrentUser()
        {
            var principal = Thread.CurrentPrincipal as CustomPrincipal ??
                            new CustomPrincipal(new CustomIdentity(PermissionsUser.Anonymous.Name, PermissionsUser.Anonymous.Id));

            return principal.ToPermissionsUser();
        }

        private void CreateMessage()
        {
            var user = GetCurrentUser();
            if (user.Equals(PermissionsUser.Anonymous))
            {
                Console.Out.WriteLine("Anonymous users can not create messages.");
                return;
            }

            Guid id = Guid.NewGuid();
            sender.SendOne(new CreateMessage
            {
                Id = id,
                Message = reader.GetString("message")
            });
        }

        private void AddNoteToMessage()
        {
            var index = storage.GetSingletonOrNew<MessageIndex>();
            var pair = reader.GetValueOf("Pick message", index.Messages, p => p.Value);
            var specification = GetCurrentUser().Spec<Note>(pair.Key).BuildFor("add");
            if (specification.IsDenied())
            {
                Console.Out.WriteLine("Error");
                Console.Out.WriteLine(specification.AuthorizationInformation);
                Console.Out.WriteLine("");
                return;
            }

            sender.SendOne(new AddNote()
            {
                MessageId = pair.Key,
                Note = reader.GetString("note")
            });
        }

        private void EditMessage()
        {
            var index = storage.GetSingletonOrNew<MessageIndex>();
            var pair = reader.GetValueOf("Pick message", index.Messages, p => p.Value);

            var specification = GetCurrentUser().Edit<Message>(pair.Key);
            if(specification.IsDenied())
            {
                Console.Out.WriteLine("Error");
                Console.Out.WriteLine(specification.AuthorizationInformation);
                Console.Out.WriteLine("");
                return;
            }

            sender.SendOne(new EditMessage
            {
                Id = pair.Key,
                Message = reader.GetString("new message"),
                OldMessage = pair.Value
            });
        }

        private void DeleteMessage()
        {
            var index = storage.GetSingletonOrNew<MessageIndex>();
            var pair = reader.GetValueOf("Pick message", index.Messages, p => p.Value);

            var specification = GetCurrentUser().Delete<Message>(pair.Key);
            if (specification.IsDenied())
            {
                Console.Out.WriteLine("Error");
                Console.Out.WriteLine(specification.AuthorizationInformation);
                return;
            }

            sender.SendOne(new DeleteMessage
            {
                Id = pair.Key
            });
        }

        private void ViewMessage()
        {
            var index = storage.GetSingletonOrNew<MessageIndex>();
            var pair = reader.GetValueOf("Pick message", index.Messages, p => p.Value);

            MessageView view;
            if(!storage.TryGetEntity(pair.Key, out view))
            {
                Console.Out.WriteLine("Message not found!");
            }

            Console.Out.WriteLine("Message");
            Console.Out.WriteLine("Created: {0}", view.UtcCreated);
            Console.Out.WriteLine("Last Modified: {0}", view.UtcLastModified);
            Console.Out.WriteLine("Body: {0}", view.Message);
            Console.Out.WriteLine("Notes:");
            foreach (var note in view.Notes)
            {
                Console.Out.WriteLine("\t{0}", note);
            }
        }
        
        public void Run()
        {
            do
            {
                Console.Clear();
                RunAs();
                Choose()();

            } while (reader.Confirm("Run more operations?"));

            token.Cancel(false);

            if (!token.Token.WaitHandle.WaitOne(5000))
            {
                Console.WriteLine("Terminating");
            }
        }

        private Action Choose()
        {
            var choice = reader.GetValueOf("Choose operation", new[]
            {
                new Tuple<string, Action>("Create message", CreateMessage),
                new Tuple<string, Action>("View message", ViewMessage),
                new Tuple<string, Action>("Edit message", EditMessage),
                new Tuple<string, Action>("Delete message", DeleteMessage),
                new Tuple<string, Action>("Add Note To Message", AddNoteToMessage)

            }, t => t.Item1);

            return choice.Item2;
        }

    }
}