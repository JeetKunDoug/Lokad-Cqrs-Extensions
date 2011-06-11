namespace Lokad.Cqrs.Extensions.Permissions
{
    public interface IEntityPermission
    {
        void Allow();
        void Deny();
    }
}