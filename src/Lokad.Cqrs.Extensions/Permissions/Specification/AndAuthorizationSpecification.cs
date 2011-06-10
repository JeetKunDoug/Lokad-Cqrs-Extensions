namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    class AndAuthorizationSpecification<T> : CompositeAuthorizationSpecification<T> where T: class , ISecurableEntity
    {
        public AndAuthorizationSpecification(IAuthorizationSpecification<T> left, IAuthorizationSpecification<T> right)
            : base(left, right)
        {
        }

        #region Overrides of CompositeAuthorizationSpecification<T>

        protected override bool IsDeniedFor(IAuthorizationSpecification<T> left, IAuthorizationSpecification<T> right)
        {
            return left.IsDenied() && right.IsDenied();
        }

        #endregion
    }
}