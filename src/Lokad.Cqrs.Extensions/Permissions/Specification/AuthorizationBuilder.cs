using System;
using System.Linq.Expressions;

using Microsoft.Practices.ServiceLocation;

using Rhino.Security.Interfaces;

namespace Lokad.Cqrs.Extensions.Permissions.Specification
{
    public sealed class AuthorizationBuilder<T> : IAuthorizationBuilder<T> where T : class, ISecurableEntity, new()
    {
        private readonly PermissionsUser user;
        private readonly Guid id;
        private readonly T entity;

        public AuthorizationBuilder(PermissionsUser user, Guid id)
        {
            this.user = user;
            this.id = id;
            entity = new T { SecurityKey = this.id };
        }

        public AuthorizationBuilder(PermissionsUser user) : this(user, Guid.Empty)
        {}

        public IAuthorizationSpecification For(Expression<Action<T>> expression)
        {
            var body = expression.Body as MethodCallExpression;
            if (body == null)
                throw new Exception("Expression is incorrect");

            return CreateSpecification(GetOperation(body.Method.Name));
        }

        public IAuthorizationSpecification For(string operation)
        {
            return CreateSpecification(GetOperation(operation));
        }

        public IAuthorizationSpecification ForRoot()
        {
            return CreateSpecification(RootOperation);
        }

        private IAuthorizationSpecification CreateSpecification(string operation)
        {
            var authorizationService = Resolve<IAuthorizationService>();

            return new AuthorizationSpecification<T>(authorizationService, entity, user, operation);
        }

        public string RootOperation
        {
            get { return string.Format("/{0}", typeof (T).Name.ToLower()); }
        }

        private string GetOperation(string action)
        {
            return string.Format("{0}/{1}", RootOperation, action).ToLower();
        }

        private TService Resolve<TService>()
        {
            return ServiceLocator.Current.GetInstance<TService>();
        }

        public PermissionsUser User
        {
            get { return user; }
        }

        public T Entity
        {
            get { return entity; }
        }
    }
}