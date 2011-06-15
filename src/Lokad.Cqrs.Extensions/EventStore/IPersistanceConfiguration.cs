using System.Data.Common;

namespace Lokad.Cqrs.Extensions.EventStore
{
    public interface IPersistanceConfiguration
    {
        string ConnectionString { get; }
        string ProviderName { get; }
        DbProviderFactory Factory { get; }
    }
}