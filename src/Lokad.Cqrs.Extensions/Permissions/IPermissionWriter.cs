using System;
using System.Collections.Generic;

using Rhino.Security;
using Rhino.Security.Interfaces;
using Rhino.Security.Model;

namespace Lokad.Cqrs.Extensions.Permissions
{
    public interface IPermissionWriter
    {
        UsersGroup AddUsersGroup(string name, string parent = null);
        TUser AddUser<TUser>(TUser user) where TUser : IUser;
        TUser AddUser<TUser>(TUser user, params UsersGroup[] groups) where TUser : IUser;
        TUser AddUser<TUser>(TUser user, params string[] groups) where TUser : IUser;
        TUser AddUserToUsersGroup<TUser>(TUser user, UsersGroup group) where TUser : IUser;
        TUser AddUserToUsersGroup<TUser>(TUser user, string group) where TUser : IUser;
        void Allow(string operation, Action<IForPermissionBuilder> config);
        void Deny(string operation, Action<IForPermissionBuilder> config);
        Operation CreateOperation(string operation);
        Operation[] GetOperations();
        Operation[] CreateOperations(IEnumerable<string> operations);
    }
}