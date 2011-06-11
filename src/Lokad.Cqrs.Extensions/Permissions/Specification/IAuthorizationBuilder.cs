namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    public interface IAuthorizationBuilder<T> where T : class, ISecurableEntity, new()
    {
        IAuthorizationSpecification<T> For(string action);
        IAuthorizationSpecification<T> For(params string[] actions);
        IAuthorizationSpecification<T> ForRoot();
        string RootOperation { get; }
    }
}