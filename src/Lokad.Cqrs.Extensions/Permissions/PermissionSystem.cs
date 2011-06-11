#region License

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
using System.Collections.Generic;
using System.Linq;

using NHibernate;

using Rhino.Security;
using Rhino.Security.Interfaces;
using Rhino.Security.Model;

namespace Lokad.Cqrs.Extensions.Permissions
{
    internal class PermissionSystem : IPermissionSystem
    {
        private readonly IPermissionsBuilderService builderService;
        private readonly IPermissionsService permissionsService;
        private readonly IAuthorizationRepository repository;
        private readonly IAuthorizationService service;
        private readonly ISession session;

        public PermissionSystem(ISession session,
                                IAuthorizationRepository repository,
                                IAuthorizationService service,
                                IPermissionsBuilderService builderService,
                                IPermissionsService permissionsService)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (repository == null) throw new ArgumentNullException("repository");
            if (service == null) throw new ArgumentNullException("service");
            if (builderService == null) throw new ArgumentNullException("builderService");
            if (permissionsService == null) throw new ArgumentNullException("permissionsService");

            this.session = session;
            this.repository = repository;
            this.service = service;
            this.builderService = builderService;
            this.permissionsService = permissionsService;
        }

        #region IPermissionSystem Members

        public UsersGroup AddUsersGroup(string name, string parent = null)
        {
            using (BeginTransaction())
            {
                return parent != null
                           ? repository.CreateChildUserGroupOf(parent, name)
                           : repository.CreateUsersGroup(name);
            }
        }

        public TUser AddUser<TUser>(TUser user) where TUser : IUser
        {
            return AddUser(user, new string[0]);
        }

        public TUser AddUser<TUser>(TUser user, params UsersGroup[] groups) where TUser : IUser
        {
            return AddUser(user, groups.Select(g => g.Name).ToArray());
        }

        public TUser AddUserToUsersGroup<TUser>(TUser user, UsersGroup @group) where TUser : IUser
        {
            return AddUserToUsersGroup(user, group.Name);
        }

        public TUser AddUser<TUser>(TUser user, params string[] groups) where TUser : IUser
        {
            using (BeginTransaction())
            {
                session.Save(user);
            }

            foreach (string group in groups)
            {
                using (BeginTransaction())
                {
                    repository.AssociateUserWith(user, group);
                }
            }

            return user;
        }

        public TUser AddUserToUsersGroup<TUser>(TUser user, string @group) where TUser : IUser
        {
            using (BeginTransaction())
            {
                repository.AssociateUserWith(user, group);
            }

            return user;
        }

        public void Allow(string operation, Action<IForPermissionBuilder> config)
        {
            using (BeginTransaction())
            {
                config(builderService.Allow(operation));
            }
        }

        public void Deny(string operation, Action<IForPermissionBuilder> config)
        {
            using (BeginTransaction())
            {
                config(builderService.Deny(operation));
            }
        }

        public Operation CreateOperation(string operation)
        {
            using (BeginTransaction())
            {
                return repository.CreateOperation(operation);
            }
        }

        public Operation[] GetOperations()
        {
            return session.QueryOver<Operation>().List().ToArray();
        }

        public Operation[] CreateOperations(IEnumerable<string> operations)
        {
            return operations.Select(CreateOperation).ToArray();
        }

        public void ClearOperations()
        {
            foreach (Operation operation in GetOperations())
            {
                ClearOperations(operation);
            }
        }

        public void Flush()
        {
            session.Flush();
        }

        public bool IsAllowed<TEntity>(IUser user, TEntity entity, string operation,
                                       out AuthorizationInformation information)
            where TEntity : class
        {
            bool result = service.IsAllowed(user, entity, operation);
            information = service.GetAuthorizationInformation(user, entity, operation);
            return result;
        }

        public bool IsAllowed(IUser user, string operation, out AuthorizationInformation information)
        {
            bool result = service.IsAllowed(user, operation);
            information = service.GetAuthorizationInformation(user, operation);
            return result;
        }

        public Permission[] GetPermissionsFor(IUser user)
        {
            return permissionsService.GetPermissionsFor(user);
        }

        public void Dispose()
        {
            session.Dispose();
        }

        #endregion

        private void ClearOperations(Operation operation)
        {
            Operation[] collections = operation.Children.ToArray();

            foreach (Operation o in collections)
            {
                repository.RemoveOperation(o.Name);
            }
            repository.RemoveOperation(operation.Name);
        }

        private IDisposable BeginTransaction()
        {
            return new DisposableAction(() => session.Flush());
        }
    }
}