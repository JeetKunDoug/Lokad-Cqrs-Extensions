namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    public interface IAuthorizationSpecificationBuilder<in T> where T : class, ISecurableEntity, new()
    {
        IAuthorizationSpecification<T> BuildFor(string action);
        IAuthorizationSpecification<T> Build();
        string RootOperation { get; }
    }
}