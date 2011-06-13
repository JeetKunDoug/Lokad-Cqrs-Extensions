using System;

using Lokad.Cqrs.Extensions.Permissions.Extensions;

using Microsoft.Practices.ServiceLocation;

using NHibernate;

using Rhino.Security.Interfaces;

namespace Lokad.Cqrs.Extensions.Permissions
{
    class EntityPermission<T> : IEntityPermission, IOperationBuilder where T : class, ISecurableEntity, new()
    {
        private readonly PermissionsUser user;
        private readonly T entity;
        private readonly IPermissionsBuilderService permissionsBuilder;
        private string activeOperation;


        public EntityPermission(PermissionsUser user, Guid entityId)
        {
            this.user = user;
            entity = new T{SecurityKey = entityId};
            permissionsBuilder = ServiceLocator.Current.GetInstance<IPermissionsBuilderService>();
        }

        public IEntityPermission OnRootOperation()
        {
            activeOperation = RootOperation;
            return this;
        }

        public IEntityPermission OnOperation(string operation)
        {
            activeOperation = GetOperation(operation);
            return this;
        }

        public void Allow()
        {
            permissionsBuilder.Allow(activeOperation).For(user).OnEntityOrEverything(entity).Level(20).Save();
            ServiceLocator.Current.GetInstance<ISession>().Flush();
        }

        public void Deny()
        {
            permissionsBuilder.Deny(activeOperation).For(user).OnEntityOrEverything(entity).Level(1).Save();
            ServiceLocator.Current.GetInstance<ISession>().Flush();
        }

        public string RootOperation
        {
            get { return string.Format("/{0}", typeof(T).Name.ToLower()); }
        }

        private string GetOperation(string action)
        {
            return string.Format("{0}/{1}", RootOperation, action).ToLower();
        }
    }
}