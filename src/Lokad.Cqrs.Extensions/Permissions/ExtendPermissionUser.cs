using System;

using Lokad.Cqrs.Extensions.Permissions.Specification;

using Microsoft.Practices.ServiceLocation;

namespace Lokad.Cqrs.Extensions.Permissions
{
    public static class ExtendPermissionUser
    {
        public static IAuthorizationBuilder<T> Authorization<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            return new AuthorizationBuilder<T>(user, id);
        }

        public static IAuthorizationSpecification<T> Edit<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            return Authorization<T>(user, id).For("edit");
        }

        public static IAuthorizationSpecification<T> Delete<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            return Authorization<T>(user, id).For("delete");
        }

        public static void TakeOwnershipOf<T>(this PermissionsUser user, Guid id) where T : class, ISecurableEntity, new()
        {
            var builder = new AuthorizationBuilder<T>(user, id);
            var specification = builder.ForRoot();
            var writer = ServiceLocator.Current.GetInstance<IPermissionWriter>();
            writer.Allow(builder.RootOperation, p=>p.For(user)
                .On(builder.Entity));
        }

        public static bool IsAnonymous(this PermissionsUser user)
        {
            return user.Equals(PermissionsUser.Anonymous);
        }

        public static bool NotAnonymous(this PermissionsUser user)
        {
            return !IsAnonymous(user);
        }
    }
}