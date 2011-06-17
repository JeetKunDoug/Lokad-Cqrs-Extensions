using System;
using System.Linq.Expressions;

namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    public interface IAuthorizationBuilder
    {
        IAuthorizationSpecification For(string operation);
        IAuthorizationSpecification ForRoot();
        string RootOperation { get; }
    }

    public interface IAuthorizationBuilder<T> : IAuthorizationBuilder where T : class, ISecurableEntity, new()
    {
        IAuthorizationSpecification For(Expression<Action<T>> expression);
    }
}