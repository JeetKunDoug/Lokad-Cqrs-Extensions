using System.Linq;

using Autofac;

using NHibernate;

using Rhino.Security.Interfaces;
using Rhino.Security.Model;

namespace Lokad.Cqrs.Extensions.Permissions
{
    public class OperationCreator : IStartable
    {
        private readonly IProvideOperations provider;
        private readonly ISession session;
        private readonly IAuthorizationRepository repository;

        public OperationCreator(IProvideOperations provider, ISession session, IAuthorizationRepository repository)
        {
            this.provider = provider;
            this.session = session;
            this.repository = repository;
        }

        #region Implementation of IStartable

        public void Start()
        {
            var existingOperations = session.QueryOver<Operation>().List().ToArray();

            foreach (var operation in provider.GetOperations().Where(operation => !existingOperations.Any(s => s.Name.Equals(operation))))
            {
                repository.CreateOperation(operation);
                session.Flush();
            }
        }

        #endregion
    }
}