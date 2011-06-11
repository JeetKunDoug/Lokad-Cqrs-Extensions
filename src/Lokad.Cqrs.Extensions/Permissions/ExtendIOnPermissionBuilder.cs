using System;

using Rhino.Security.Interfaces;

namespace Lokad.Cqrs.Extensions.Permissions
{
    internal static class ExtendIOnPermissionBuilder
    {
        public static ILevelPermissionBuilder OnEntityOrEverything<T>(this IOnPermissionBuilder builder, T entity) where T: class, ISecurableEntity
        {
            return entity.SecurityKey.Equals(Guid.Empty) ? builder.OnEverything() : builder.On(entity);
        }
    }
}