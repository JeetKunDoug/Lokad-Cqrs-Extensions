using System;

using Lokad.Cqrs.Extensions.Permissions.Specification;

using Microsoft.Practices.ServiceLocation;

namespace Lokad.Cqrs.Extensions.Permissions
{
    public static class ExtendPermissionUser
    {
        private static IPermissionSystem Permissions
        {
            get { return ServiceLocator.Current.GetInstance<IPermissionSystem>(); }
        }

        public static IAuthorizationSpecificationBuilder<T> Spec<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            return new AuthorizationSpecificationBuilder<T>(Permissions, user, id);
        }

        public static IAuthorizationSpecification<T> Edit<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            return new AuthorizationSpecificationBuilder<T>(Permissions, user, id).BuildFor("edit");
        }

        public static IAuthorizationSpecification<T> Delete<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            return new AuthorizationSpecificationBuilder<T>(Permissions, user, id).BuildFor("delete");
        }

        public static void TakeOwnership<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            var builder = new AuthorizationSpecificationBuilder<T>(Permissions, user, id);
            Permissions.Allow(builder.RootOperation, p => p.For(user).On(new T {SecurityKey = id}).DefaultLevel().Save());
        }
    }
}