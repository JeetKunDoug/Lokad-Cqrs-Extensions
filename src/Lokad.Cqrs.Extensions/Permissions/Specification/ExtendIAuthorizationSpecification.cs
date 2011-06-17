namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    public static class ExtendIAuthorizationSpecification
    {
        public static IAuthorizationSpecification And(this IAuthorizationSpecification @this, IAuthorizationSpecification specification)
        {
            return new AndAuthorizationSpecification(@this, specification);
        }

        public static IAuthorizationSpecification Or(this IAuthorizationSpecification @this, IAuthorizationSpecification specification)
        {
            return new OrAuthorizationSpecification(@this, specification);
        }
    }
}