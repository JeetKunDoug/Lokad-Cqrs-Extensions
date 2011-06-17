namespace Lokad.Cqrs.Extensions.Permissions
{
    public interface IOperationBuilder
    {
        IEntityPermission OnOperation(string operation);
        IEntityPermission OnRootOperation();
        void AllowOperations(params string[] operations);
        void DenyOperations(params string[] operations);
    }
}