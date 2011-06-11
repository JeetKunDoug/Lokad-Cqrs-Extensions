using System;

namespace Lokad.Cqrs.Extensions.Permissions
{
    public class PermissionBuilder
    {
        private readonly PermissionsUser user;

        public PermissionBuilder(PermissionsUser user)
        {
            this.user = user;
        }

        public IOperationBuilder ForEntity<T>(Guid id) where T : class, ISecurableEntity, new()
        {
            return new EntityPermission<T>(user, id);
        }

        public IOperationBuilder ForEntity<T>() where T : class, ISecurableEntity, new()
        {
            return ForEntity<T>(Guid.Empty);
        }
    }
}