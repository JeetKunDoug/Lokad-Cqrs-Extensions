using System;

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

        public IAuthorizationSpecification<T> For(string action)
        {
            return CreateSpecification(GetOperation(action));
        }

        public IAuthorizationSpecification<T> For(params string[] actions)
        {
            IAuthorizationSpecification<T> specification = new NullAuthorizationSpecification<T>();

            if(actions.Length > 0)
            {
                specification = CreateSpecification(GetOperation(actions[0]));
            }

            for (int i = 1; i < actions.Length; i++)
            {
                var operation = GetOperation(actions[i]);
                var child = CreateSpecification(operation);
                specification = specification.And(child);
            }

            return specification;
        }

        public IAuthorizationSpecification<T> ForRoot()
        {
            return CreateSpecification(RootOperation);
        }

        private IAuthorizationSpecification<T> CreateSpecification(string operation)
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