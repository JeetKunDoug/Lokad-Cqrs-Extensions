namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    class OrAuthorizationSpecification<T> : CompositeAuthorizationSpecification<T> where T : class, ISecurableEntity
    {
        public OrAuthorizationSpecification(IAuthorizationSpecification<T> left, IAuthorizationSpecification<T> right)
            : base(left, right)
        {
        }

        #region Overrides of CompositeAuthorizationSpecification<T>

        protected override bool IsDeniedFor(IAuthorizationSpecification<T> left, IAuthorizationSpecification<T> right)
        {
            return left.IsDenied() || right.IsDenied();
        }

        #endregion
    }
}