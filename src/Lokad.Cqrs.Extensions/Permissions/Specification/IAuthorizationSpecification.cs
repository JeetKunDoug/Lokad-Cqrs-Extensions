namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    public interface IAuthorizationSpecification<in T> where T : class, ISecurableEntity
    {
        bool IsDenied();
        string AuthorizationInformation { get; }
        void Allow();
        void Deny();
    }
}