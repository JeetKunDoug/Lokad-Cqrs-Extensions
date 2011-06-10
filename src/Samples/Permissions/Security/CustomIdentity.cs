using System;
using System.Security.Principal;

using Rhino.Security;

namespace Security
{
    public class CustomIdentity : IIdentity
    {
        private readonly Guid id;
        private readonly string name;

        public CustomIdentity(string name, Guid id)
        {
            this.name = name;
            this.id = id;
        }

        #region Implementation of IIdentity

        public string Name
        {
            get { return name; }
        }

        public string AuthenticationType
        {
            get { return "Custom"; }
        }

        public bool IsAuthenticated
        {
            get { return id != Guid.Empty; }
        }

        #endregion

        public Guid ID
        {
            get { return id; }
        }
    }
}