using Rhino.Security;

namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    class AuthorizationSpecification<T> : IAuthorizationSpecification<T> where T : class, ISecurableEntity
    {
        private readonly string operation;
        private readonly IPermissionSystem permissions;
        private readonly T entity;
        private readonly PermissionsUser user;

        public AuthorizationSpecification(IPermissionSystem permissions, T entity, PermissionsUser user, string operation)
        {
            this.operation = operation;
            this.permissions = permissions;
            this.entity = entity;
            this.user = user;
        }

        #region Implementation of IAuthorizationSpecification<T>

        public virtual bool IsDenied()
        {
            AuthorizationInformation info;
            var result = permissions.IsAllowed(user, entity, operation, out info);
            AuthorizationInformation = info.ToString();
            return !result;
        }

        public virtual string AuthorizationInformation { get; private set; }
        
        public void Allow()
        {
            permissions.Allow(operation, p => p.For(user).On(entity).DefaultLevel().Save());
        }

        public void Deny()
        {
            permissions.Deny(operation, p => p.For(user).On(entity).DefaultLevel().Save());
        }

        #endregion
    }
}