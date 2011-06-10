using System;

namespace Lokad.Cqrs.Extensions.Permissions.Securables
{
    public class SecurableEntity : ISecurableEntity
    {
        private readonly Guid id;

        internal SecurableEntity(Guid id)
        {
            this.id = id;
        }

        public Guid SecurityKey
        {
            get { return id; }
        }

        public static class For
        {
            public static Event Event(Guid id)
            {
                return new Event(id);
            }
        }
    }
}