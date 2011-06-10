using System.Linq;

using Autofac;

namespace Lokad.Cqrs.Extensions.Permissions.Build
{
    public class OperationCreator : IStartable
    {
        private readonly IProvideOperations provider;
        private readonly IPermissionWriter writer;

        public OperationCreator(IProvideOperations provider, IPermissionWriter writer)
        {
            this.provider = provider;
            this.writer = writer;
        }

        #region Implementation of IStartable

        public void Start()
        {
            var existingOperations = writer.GetOperations().Select(o => o.Name).ToArray();

            foreach (var operation in provider.GetOperations().Where(operation => !existingOperations.Any(s => s.Equals(operation))))
            {
                writer.CreateOperation(operation);
            }
        }

        #endregion
    }
}