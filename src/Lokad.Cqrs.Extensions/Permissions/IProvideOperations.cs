using System.Collections.Generic;

namespace Lokad.Cqrs.Extensions.Permissions
{
    public interface IProvideOperations
    {
        IEnumerable<string> GetOperations();
    }
}