using Rhino.Security;

namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    abstract class CompositeAuthorizationSpecification<T> : IAuthorizationSpecification<T> where T: class, ISecurableEntity
    {
        private readonly IAuthorizationSpecification<T> left;
        private readonly IAuthorizationSpecification<T> right;
        private readonly AuthorizationInformation information;

        protected CompositeAuthorizationSpecification(IAuthorizationSpecification<T> left, IAuthorizationSpecification<T> right)
        {
            this.left = left;
            this.right = right;
            information = new AuthorizationInformation();
        }

        #region Implementation of IAuthorizationSpecification<T>

        public bool IsDenied()
        {
            var result = IsDeniedFor(left, right);
            information.AddAllow(left.AuthorizationInformation);
            information.AddAllow(right.AuthorizationInformation);
            return result;
        }

        public string AuthorizationInformation
        {
            get { return information.ToString(); }
        }

        public void Allow()
        {
            left.Allow();
            right.Allow();
        }

        public void Deny()
        {
            left.Deny();
            right.Deny();
        }

        #endregion

        protected abstract bool IsDeniedFor(IAuthorizationSpecification<T> left,
                                            IAuthorizationSpecification<T> right);
    }
}