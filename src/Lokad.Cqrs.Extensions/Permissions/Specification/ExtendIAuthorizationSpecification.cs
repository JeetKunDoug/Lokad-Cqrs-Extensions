namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    public static class ExtendIAuthorizationSpecification
    {
        public static IAuthorizationSpecification<T> And<T>(this IAuthorizationSpecification<T> @this, IAuthorizationSpecification<T> specification) where T : class, ISecurableEntity
        {
            return new AndAuthorizationSpecification<T>(@this, specification);
        }

        public static IAuthorizationSpecification<T> Or<T>(this IAuthorizationSpecification<T> @this, IAuthorizationSpecification<T> specification) where T : class, ISecurableEntity
        {
            return new OrAuthorizationSpecification<T>(@this, specification);
        }

        public static IAuthorizationSpecification<T> Not<T>(this IAuthorizationSpecification<T> @this) where T : class, ISecurableEntity
        {
            return new NotAuthorizationSpecification<T>(@this);
        }
    }
}