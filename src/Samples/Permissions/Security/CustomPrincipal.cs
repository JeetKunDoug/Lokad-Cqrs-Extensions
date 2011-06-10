using System.Security.Principal;

using Lokad.Cqrs.Extensions.Permissions;

namespace Security
{
    public class CustomPrincipal : IPrincipal
    {
        public CustomPrincipal(CustomIdentity identity)
        {
            Identity = identity;
        }

        #region Implementation of IPrincipal

        public bool IsInRole(string role)
        {
            return true;
        }

        public IIdentity Identity { get; private set; }

        #endregion

        public CustomIdentity CustomIdentity
        {
            get { return (CustomIdentity) Identity; }
        }

        public PermissionsUser ToPermissionsUser()
        {
            var identity = (CustomIdentity)Identity;
            return new PermissionsUser(identity.Name, identity.ID);
        }
    }
}