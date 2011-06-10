using System;

namespace Lokad.Cqrs.Extensions.Permissions.Securables
{
    public class Event : SecurableEntity
    {
        internal Event(Guid id) : base(id)
        {
        }
    }
}