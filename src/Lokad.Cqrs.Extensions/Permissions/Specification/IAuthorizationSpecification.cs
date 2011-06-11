namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    public interface IAuthorizationSpecification<T> where T : class, ISecurableEntity
    {
        bool IsDenied();
        bool IsAllowed();
        string AuthorizationInformation { get; }
    }
}