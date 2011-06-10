using System.Collections.Generic;

using Lokad.Cqrs.Extensions.Permissions;

namespace InMemoryClient
{
    public class OperationProvider : IProvideOperations
    {
        #region Implementation of IProvideOperations

        public IEnumerable<string> GetOperations()
        {
            yield return "/message/create";
            yield return "/message/edit";
            yield return "/message/view";
            yield return "/message/delete";
            yield return "/note/add";
        }

        #endregion
    }
}