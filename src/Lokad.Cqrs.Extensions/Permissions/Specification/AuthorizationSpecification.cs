using Rhino.Security.Interfaces;

namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    class AuthorizationSpecification<T> : IAuthorizationSpecification<T> where T : class, ISecurableEntity
    {
        private readonly string operation;
        private readonly IAuthorizationService authorizationService;
        private readonly T entity;
        private readonly PermissionsUser user;

        public AuthorizationSpecification(IAuthorizationService authorizationService, T entity, PermissionsUser user, string operation)
        {
            this.operation = operation;
            this.authorizationService = authorizationService;
            this.entity = entity;
            this.user = user;
        }

        #region Implementation of IAuthorizationSpecification<T>

        public virtual bool IsDenied()
        {
            return !IsAllowed();
        }

        public bool IsAllowed()
        {
            return authorizationService.IsAllowed(user, entity, operation);
        }

        public virtual string AuthorizationInformation
        {
            get { return authorizationService.GetAuthorizationInformation(user, entity, operation).ToString(); }
        }
        
        #endregion
    }
}