namespace Lokad.Cqrs.Extensions.Permissions
{
    public interface IOperationBuilder
    {
        IEntityPermission OnOperation(string operation);
        IEntityPermission OnRootOperation();
    }
}