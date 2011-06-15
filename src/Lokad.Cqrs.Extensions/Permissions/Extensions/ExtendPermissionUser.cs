using System;

using Lokad.Cqrs.Extensions.Permissions.Specification;

using Microsoft.Practices.ServiceLocation;

using NHibernate;

namespace Lokad.Cqrs.Extensions.Permissions.Extensions
{
    public static class ExtendPermissionUser
    {
        public static IAuthorizationBuilder<T> Authorization<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            return new AuthorizationBuilder<T>(user, id);
        }
        
        public static IAuthorizationBuilder<T> Authorization<T>(this PermissionsUser user) where T : class, ISecurableEntity, new()
        {
            return new AuthorizationBuilder<T>(user);
        }

        public static IAuthorizationSpecification<T> Edit<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            return Authorization<T>(user, id).For("edit");
        }

        public static IAuthorizationSpecification<T> Edit<T>(this PermissionsUser user) where T : class, ISecurableEntity, new()
        {
            return Authorization<T>(user).For("edit");
        }

        public static IAuthorizationSpecification<T> Delete<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            return Authorization<T>(user, id).For("delete");
        }

        public static void TakeOwnershipOf<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            user.Permission().ForEntity<T>(id).OnRootOperation().Allow();
        }

        public static bool IsAnonymous(this PermissionsUser user)
        {
            return user.Equals(PermissionsUser.Anonymous);
        }

        public static bool NotAnonymous(this PermissionsUser user)
        {
            return !IsAnonymous(user);
        }

        public static PermissionsUser Save(this PermissionsUser user)
        {
            var session = ServiceLocator.Current.GetInstance<ISession>();
            session.Save(user);
            session.Flush();
            return user;
        }

        public static PermissionBuilder Permission(this PermissionsUser user)
        {
            return new PermissionBuilder(user);
        }
    }
}