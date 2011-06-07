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

        public static AggregateTransaction<TAggregate> BeginAggregateTransaction<TAggregate>(this IRepository repository, Guid id) where TAggregate : class, IAggregate
        {
            return BeginAggregateTransaction<TAggregate>(repository, id, int.MaxValue);
        }

        public static AggregateTransaction<TAggregate> BeginAggregateTransaction<TAggregate>(this IRepository repository, Guid id, Action<IDictionary<string, object>> updateHeaders) where TAggregate : class, IAggregate
        {
            return BeginAggregateTransaction<TAggregate>(repository, id, int.MaxValue, headers => { });
        }

        public static AggregateTransaction<TAggregate> BeginAggregateTransaction<TAggregate>(this IRepository repository, Guid id, int version) where TAggregate : class, IAggregate
        {
            return BeginAggregateTransaction<TAggregate>(repository, id, version, headers=>{});
        }

        public static AggregateTransaction<TAggregate> BeginAggregateTransaction<TAggregate>(this IRepository repository, Guid id, int version, Action<IDictionary<string, object>> updateHeaders) where TAggregate : class, IAggregate
        {
            var aggregate = repository.GetById<TAggregate>(id, version);

            return new AggregateTransaction<TAggregate>(repository, aggregate, updateHeaders);
        }
    }
}