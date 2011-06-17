namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    public interface IAuthorizationSpecification
    {
        bool IsDenied();
        bool IsAllowed();
        string AuthorizationInformation { get; }
        void Demand();
    }

    public interface IAuthorizationSpecification<T> : IAuthorizationSpecification
        where T: class, ISecurableEntity, new()
    {}
}