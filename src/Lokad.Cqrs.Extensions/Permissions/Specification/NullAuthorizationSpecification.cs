namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    class NullAuthorizationSpecification<T> : IAuthorizationSpecification<T> where T : class, ISecurableEntity
    {
        public bool IsDenied()
        {
            return false;
        }

        public bool IsAllowed()
        {
            return true;
        }

        public string AuthorizationInformation
        {
            get { return string.Empty; }
        }
    }
}