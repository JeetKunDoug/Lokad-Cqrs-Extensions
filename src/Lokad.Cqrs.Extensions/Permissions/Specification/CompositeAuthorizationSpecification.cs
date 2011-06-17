using Rhino.Security;

namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    abstract class CompositeAuthorizationSpecification : IAuthorizationSpecification
    {
        private readonly IAuthorizationSpecification left;
        private readonly IAuthorizationSpecification right;

        protected CompositeAuthorizationSpecification(IAuthorizationSpecification left, IAuthorizationSpecification right)
        {
            this.left = left;
            this.right = right;
        }

        #region Implementation of IAuthorizationSpecification<T>

        public bool IsDenied()
        {
            return !IsAllowed();
        }

        public bool IsAllowed()
        {
            return IsAllowed(left, right);
        }

        public string AuthorizationInformation
        {
            get
            {
                var information = new AuthorizationInformation();
                information.AddAllow(left.AuthorizationInformation);
                information.AddAllow(right.AuthorizationInformation);
                return information.ToString();
            }
        }

        public void Demand()
        {
            if (IsDenied())
                throw new PermissionException(AuthorizationInformation);
        }

        #endregion

        protected abstract bool IsAllowed(IAuthorizationSpecification left,
                                            IAuthorizationSpecification right);
    }
}