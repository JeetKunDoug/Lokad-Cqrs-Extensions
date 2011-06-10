namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    class NotAuthorizationSpecification<T> : IAuthorizationSpecification<T> where T : class, ISecurableEntity
    {
        private readonly IAuthorizationSpecification<T> specification;

        public NotAuthorizationSpecification(IAuthorizationSpecification<T> specification)
        {
            this.specification = specification;
        }

        public bool IsDenied()
        {
            return !specification.IsDenied();
        }

        public string AuthorizationInformation
        {
            get { return specification.AuthorizationInformation; }
        }

        public void Allow()
        {
            specification.Deny();
        }

        public void Deny()
        {
            specification.Allow();
        }
    }
}