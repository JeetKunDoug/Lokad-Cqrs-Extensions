using System;

namespace Lokad.Cqrs.Extensions.Permissions
{
    [Serializable]
    public class PermissionException : Exception
    {
        public PermissionException(string message) : base(message)
        {
        }
    }
}