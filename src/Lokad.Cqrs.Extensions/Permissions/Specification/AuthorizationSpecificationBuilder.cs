using System;

namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    public sealed class AuthorizationSpecificationBuilder<T> : IAuthorizationSpecificationBuilder<T> where T : class, ISecurableEntity, new()
    {
        private readonly IPermissionSystem permissions;
        private readonly PermissionsUser user;
        private readonly Guid id;

        public AuthorizationSpecificationBuilder(IPermissionSystem permissions, PermissionsUser user, Guid id)
        {
            this.permissions = permissions;
            this.user = user;
            this.id = id;
        }

        public IAuthorizationSpecification<T> BuildFor(string action)
        {
            return CreateSpecification(GetOperation(action));
        }

        public IAuthorizationSpecification<T> Build()
        {
            return CreateSpecification(RootOperation);
        }

        private IAuthorizationSpecification<T> CreateSpecification(string operation)
        {
            var entity = new T { SecurityKey = id };
            return new AuthorizationSpecification<T>(permissions, entity, user, operation);
        }

        public string RootOperation
        {
            get { return string.Format("/{0}", typeof (T).Name.ToLower()); }
        }

        private string GetOperation(string action)
        {
            return string.Format("{0}/{1}", RootOperation, action).ToLower();
        }
    }
}