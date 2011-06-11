using Rhino.Security;

namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    abstract class CompositeAuthorizationSpecification<T> : IAuthorizationSpecification<T> where T: class, ISecurableEntity
    {
        private readonly IAuthorizationSpecification<T> left;
        private readonly IAuthorizationSpecification<T> right;

        protected CompositeAuthorizationSpecification(IAuthorizationSpecification<T> left, IAuthorizationSpecification<T> right)
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

        #endregion

        protected abstract bool IsAllowed(IAuthorizationSpecification<T> left,
                                            IAuthorizationSpecification<T> right);
    }
}