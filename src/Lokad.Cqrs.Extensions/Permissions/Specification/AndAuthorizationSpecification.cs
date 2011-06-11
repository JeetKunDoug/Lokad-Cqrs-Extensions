namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    class AndAuthorizationSpecification<T> : CompositeAuthorizationSpecification<T> where T: class , ISecurableEntity
    {
        public AndAuthorizationSpecification(IAuthorizationSpecification<T> left, IAuthorizationSpecification<T> right)
            : base(left, right)
        {
        }

        #region Overrides of CompositeAuthorizationSpecification<T>

        protected override bool IsAllowed(IAuthorizationSpecification<T> left, IAuthorizationSpecification<T> right)
        {
            return left.IsAllowed() && right.IsAllowed();
        }

        #endregion
    }
}