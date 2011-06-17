namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    class NullAuthorizationSpecification : IAuthorizationSpecification
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

        public void Demand()
        {}
    }
}