using Rhino.Security;
using Rhino.Security.Model;

namespace Lokad.Cqrs.Extensions.Permissions
{
    public interface IPermissionReader
    {
        bool IsAllowed<TEntity>(IUser user, TEntity entity, string operation, out AuthorizationInformation information)
            where TEntity : class;

        bool IsAllowed(IUser user, string operation, out AuthorizationInformation information);

        Permission[] GetPermissionsFor(IUser user);
    }
}