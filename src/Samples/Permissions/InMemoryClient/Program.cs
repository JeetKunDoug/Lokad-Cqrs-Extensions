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
using Lokad.Cqrs.Extensions.Permissions.Specification;
using Lokad.Cqrs.Feature.AtomicStorage;

using Rhino.Security.Interfaces;

using Security;

using Shared;

using Views;

namespace InMemoryClient
{
    internal class Program
    {
        private readonly PermissionsUser admin;
        private readonly ConsoleReader reader;
        private readonly IMessageSender sender;
        private readonly NuclearStorage storage;
        private readonly CancellationTokenSource token;
        private readonly IAuthorizationRepository authorizationRepository;
        private readonly IEnumerable<PermissionsUser> users;

        public Program(IMessageSender sender, NuclearStorage storage, CancellationTokenSource token, IAuthorizationRepository authorizationRepository)
        {
            this.sender = new SecuritySenderDecorator(new SenderDecorator(sender));
            this.storage = storage;
            this.token = token;
            this.authorizationRepository = authorizationRepository;

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
            foreach (PermissionsUser user in users)
            {
                user.Save();
            }

            admin.Permission().ForEntity<Message>().OnRootOperation().Allow();
        }

        private void RunAs()
        {
            PermissionsUser user = reader.GetValueOf("Run as", users, u => u.Name);

            var identity = new CustomIdentity(user.Name, user.Id);
            Thread.CurrentPrincipal = new CustomPrincipal(identity);
            Console.Clear();
            Console.Out.WriteLine("Running as: {0}", identity.Name);
        }

        private PermissionsUser GetCurrentUser()
        {
            CustomPrincipal principal = Thread.CurrentPrincipal as CustomPrincipal ??
                                        new CustomPrincipal(new CustomIdentity(PermissionsUser.Anonymous.Name,
                                                                               PermissionsUser.Anonymous.Id));

            return principal.ToPermissionsUser();
        }

        private void CreateMessage()
        {
            PermissionsUser user = GetCurrentUser();
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
            KeyValuePair<Guid, string> pair = reader.GetValueOf("Pick message", index.Messages, p => p.Value);
            IAuthorizationSpecification<Note> specification = GetCurrentUser().Authorization<Note>(pair.Key).For("add");
            if (specification.IsDenied())
            {
                Console.Out.WriteLine("Error");
                Console.Out.WriteLine(specification.AuthorizationInformation);
                Console.Out.WriteLine("");
                return;
            }

            sender.SendOne(new AddNote
                {
                    MessageId = pair.Key,
                    Note = reader.GetString("note")
                });
        }

        private void EditMessage()
        {
            var index = storage.GetSingletonOrNew<MessageIndex>();
            KeyValuePair<Guid, string> pair = reader.GetValueOf("Pick message", index.Messages, p => p.Value);

            IAuthorizationSpecification<Message> specification = GetCurrentUser().Edit<Message>(pair.Key);
            if (specification.IsDenied())
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
            KeyValuePair<Guid, string> pair = reader.GetValueOf("Pick message", index.Messages, p => p.Value);

            IAuthorizationSpecification<Message> specification = GetCurrentUser().Delete<Message>(pair.Key);
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
            KeyValuePair<Guid, string> pair = reader.GetValueOf("Pick message", index.Messages, p => p.Value);

            MessageView view;
            if (!storage.TryGetEntity(pair.Key, out view))
            {
                Console.Out.WriteLine("Message not found!");
            }

            Console.Out.WriteLine("Message");
            Console.Out.WriteLine("Created: {0}", view.UtcCreated);
            Console.Out.WriteLine("Last Modified: {0}", view.UtcLastModified);
            Console.Out.WriteLine("Body: {0}", view.Message);
            Console.Out.WriteLine("Notes:");
            foreach (string note in view.Notes)
            {
                Console.Out.WriteLine("\t{0}", note);
            }
        }

        public void Run()
        {
            Guid id = Guid.NewGuid();

            do
            {
                Console.Clear();
                RunAs();
                Choose(id)();
            } while (reader.Confirm("Run more operations?"));

            token.Cancel(false);

            if (!token.Token.WaitHandle.WaitOne(5000))
            {
                Console.WriteLine("Terminating");
            }
        }

        private Action Choose(Guid id)
        {
            Tuple<string, Action> choice = reader.GetValueOf("Choose operation", new[]
                {
                    new Tuple<string, Action>("Create message", CreateMessage),
                    new Tuple<string, Action>("View message", ViewMessage),
                    new Tuple<string, Action>("Edit message", EditMessage),
                    new Tuple<string, Action>("Delete message", DeleteMessage),
                    new Tuple<string, Action>("Add Note To Message", AddNoteToMessage),
                    new Tuple<string, Action>("Deny op", ()=>DenyOperation(id)), 
                    new Tuple<string, Action>("Allow op", ()=>AllowOperation(id)), 
                    new Tuple<string, Action>("Allow multiple ops", () => AllowMultipleOperations(id)),
                    new Tuple<string, Action>("Deny multiple ops", () => DenyMultipleOperations(id)),
                    new Tuple<string, Action>("Allow, deny, and check", () => AllowDenyAndCheck(id))
                    
                }, t => t.Item1);

            return choice.Item2;
        }

        private void AllowDenyAndCheck(Guid id)
        {
            PermissionsUser user = GetCurrentUser();

            var builder = user.Permission().ForEntity<Note>(id);
            builder.OnOperation("add").Allow();
            builder.OnOperation("delete").Deny();

            var deleteSpec = user.Authorization<Note>(id).For("delete");
            var addSpec = user.Authorization<Note>(id).For("add");

            var orSpecification = addSpec.Or(deleteSpec);
            var andSpecification = addSpec.And(deleteSpec);
            
            Console.Out.WriteLine("");
            Console.Out.WriteLine("orSpecification is allowed: {0}", orSpecification.IsAllowed());
            Console.Out.WriteLine(deleteSpec.AuthorizationInformation);
            Console.Out.WriteLine("");
            Console.Out.WriteLine("andSpecification is allowed: {0}", andSpecification.IsAllowed());
            Console.Out.WriteLine(addSpec.AuthorizationInformation);
        }

        private void AllowMultipleOperations(Guid id)
        {
            PermissionsUser user = GetCurrentUser();

            var builder = user.Permission().ForEntity<Note>(id);

            builder.OnOperation("add").Allow();
            builder.OnOperation("delete").Allow();
            builder.OnOperation("create").Allow();
            builder.OnOperation("view").Allow();
            builder.OnOperation("do").Allow();
            builder.OnOperation("edit").Allow();
        }

        private void DenyOperation(Guid id)
        {
            PermissionsUser user = GetCurrentUser();
            var op = reader.GetValueOf("operation", new[]{"add", "delete", "create",
                                                               "view", "do", "edit"}, s => s);


            var builder = user.Permission().ForEntity<Note>(id);

            builder.OnOperation(op).Deny();
        }

        private void AllowOperation(Guid id)
        {
            PermissionsUser user = GetCurrentUser();
            var op = reader.GetValueOf("operation", new[]{"add", "delete", "create",
                                                               "view", "do", "edit"}, s => s);


            var builder = user.Permission().ForEntity<Note>(id);

            builder.OnOperation(op).Allow();
        }

        private void DenyMultipleOperations(Guid id)
        {
            PermissionsUser user = GetCurrentUser();

            var builder = user.Permission().ForEntity<Note>(id);

            builder.OnOperation("add").Deny();
            builder.OnOperation("delete").Deny();
            builder.OnOperation("create").Deny();
            builder.OnOperation("view").Deny();
            builder.OnOperation("do").Deny();
            builder.OnOperation("edit").Deny();
        }
    }
}