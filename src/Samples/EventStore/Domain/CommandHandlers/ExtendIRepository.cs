using System;
using System.Collections.Generic;

using CommonDomain;
using CommonDomain.Persistence;

namespace Domain.CommandHandlers
{
    public static class ExtendIRepository
    {
        public static void Save(this IRepository @this, IAggregate aggregate)
        {
            @this.Save(aggregate, Guid.NewGuid(), d => { });
        }
        public static void Save(this IRepository @this, IAggregate aggregate, Action<IDictionary<string, object>> updateHeaders)
        {
            @this.Save(aggregate, Guid.NewGuid(), updateHeaders);
        }
    }
}