namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    class AndAuthorizationSpecification : CompositeAuthorizationSpecification
    {
        public AndAuthorizationSpecification(IAuthorizationSpecification left, IAuthorizationSpecification right)
            : base(left, right)
        {
        }

        #region Overrides of CompositeAuthorizationSpecification<T>

        protected override bool IsAllowed(IAuthorizationSpecification left, IAuthorizationSpecification right)
        {
            return left.IsAllowed() && right.IsAllowed();
        }

        #endregion
    }
}