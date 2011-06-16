using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Runtime.Serialization;

using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Azure;

namespace Lokad.Cqrs.Extensions.Storage
{
    /// <summary>Mock in-memory TableStorage Provider.</summary>
    /// <remarks>
    /// All the methods of <see cref="FileTableStorageProvider"/> are thread-safe.
    /// </remarks>
    public class FileTableStorageProvider : ITableStorageProvider
    {
        /// <summary>In memory table storage : entries per table (designed for simplicity instead of performance)</summary>
        readonly FileTableStore tables;

        /// <summary>Formatter as requiered to handle FatEntities.</summary>
        internal Cloud.Storage.Shared.IDataSerializer DataSerializer { get; set; }

        /// <summary>naive global lock to make methods thread-safe.</summary>
        readonly object syncRoot;

        int nextETag;

        /// <summary>
        /// Constructor for <see cref="FileTableStorageProvider"/>.
        /// </summary>
        public FileTableStorageProvider()
        {
            tables = new FileTableStore();
            syncRoot = new object();
            DataSerializer = new CloudFormatter();
        }

        /// <see cref="ITableStorageProvider.CreateTable"/>
        public bool CreateTable(string tableName)
        {
            lock (syncRoot)
            {
                if (tables.ContainsKey(tableName))
                {
                    //If the table already exists: return false.
                    return false;
                }

                //create table return true.
                tables.Add(tableName, new List<MockTableEntry>());
                return true;
            }
        }

        /// <see cref="ITableStorageProvider.DeleteTable"/>
        public bool DeleteTable(string tableName)
        {
            lock (syncRoot)
            {
                if (tables.ContainsKey(tableName))
                {
                    //If the table exists remove it.
                    tables.Remove(tableName);
                    return true;
                }

                //Can not remove an unexisting table.
                return false;
            }
        }

        /// <see cref="ITableStorageProvider.GetTables"/>
        public IEnumerable<string> GetTables()
        {
            lock (syncRoot)
            {
                return tables.Keys;
            }
        }

        /// <see cref="ITableStorageProvider.Get{T}(string)"/>
        IEnumerable<CloudEntity<T>> GetInternal<T>(string tableName, Func<MockTableEntry, bool> predicate)
        {
            lock (syncRoot)
            {
                if (!tables.ContainsKey(tableName))
                {
                    return new List<CloudEntity<T>>();
                }

                return from entry in tables[tableName]
                       where predicate(entry)
                       select entry.ToCloudEntity<T>(DataSerializer);
            }
        }

        /// <see cref="ITableStorageProvider.Get{T}(string)"/>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName)
        {
            return GetInternal<T>(tableName, entry => true);
        }

        /// <see cref="ITableStorageProvider.Get{T}(string,string)"/>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey)
        {
            return GetInternal<T>(tableName, entry => entry.PartitionKey == partitionKey);
        }

        /// <see cref="ITableStorageProvider.Get{T}(string,string,string,string)"/>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, string startRowKey, string endRowKey)
        {
            var isInRange = string.IsNullOrEmpty(endRowKey)
                                ? (Func<string, bool>)(rowKey => string.Compare(startRowKey, rowKey) <= 0)
                                : (rowKey => string.Compare(startRowKey, rowKey) <= 0 && string.Compare(rowKey, endRowKey) < 0);

            return GetInternal<T>(tableName, entry => entry.PartitionKey == partitionKey && isInRange(entry.RowKey))
                .OrderBy(entity => entity.RowKey);
        }

        /// <see cref="ITableStorageProvider.Get{T}(string,string,System.Collections.Generic.IEnumerable{string})"/>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys)
        {
            var keys = new HashSet<string>(rowKeys);
            return GetInternal<T>(tableName, entry => entry.PartitionKey == partitionKey && keys.Contains(entry.RowKey));
        }

        /// <see cref="ITableStorageProvider.Insert{T}"/>
        public void Insert<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
        {
            lock (syncRoot)
            {
                List<MockTableEntry> entries;
                if (!tables.TryGetValue(tableName, out entries))
                {
                    tables.Add(tableName, entries = new List<MockTableEntry>());
                }

                var list = entities.ToArray();

                // verify valid data BEFORE inserting them
                if (list.Join(entries, ToId, ToId, (u, v) => true).Any())
                {
                    throw new DataServiceRequestException("INSERT: key conflict.");
                }
                if (list.GroupBy(ToId).Any(id => id.Count() != 1))
                {
                    throw new DataServiceRequestException("INSERT: duplicate keys.");
                }

                // ok, we can insert safely now
                foreach (var entity in list)
                {
                    var etag = (nextETag++).ToString();
                    entity.ETag = etag;
                    entries.Add(new MockTableEntry
                    {
                        PartitionKey = entity.PartitionKey,
                        RowKey = entity.RowKey,
                        ETag = etag,
                        Value = FatEntity.Convert(entity, DataSerializer)
                    });
                }

                tables.InsertInto(tableName, entries);
            }
        }

        /// <see cref="ITableStorageProvider.Update{T}"/>
        public void Update<T>(string tableName, IEnumerable<CloudEntity<T>> entities, bool force)
        {
            lock (syncRoot)
            {
                List<MockTableEntry> entries;
                if (!tables.TryGetValue(tableName, out entries))
                {
                    throw new DataServiceRequestException("UPDATE: table not found.");
                }

                var list = entities.ToArray();

                // verify valid data BEFORE updating them
                if (list.GroupJoin(entries, ToId, ToId, (u, vs) => vs.Count(entry => force || u.ETag == null || entry.ETag == u.ETag)).Any(c => c != 1))
                {
                    throw new DataServiceRequestException("UPDATE: key not found or etag conflict.");
                }
                if (list.GroupBy(ToId).Any(id => id.Count() != 1))
                {
                    throw new DataServiceRequestException("UPDATE: duplicate keys.");
                }

                // ok, we can update safely now
                foreach (var entity in list)
                {
                    var etag = (nextETag++).ToString();
                    entity.ETag = etag;
                    var index = entries.FindIndex(entry => entry.PartitionKey == entity.PartitionKey && entry.RowKey == entity.RowKey);
                    entries[index] = new MockTableEntry
                    {
                        PartitionKey = entity.PartitionKey,
                        RowKey = entity.RowKey,
                        ETag = etag,
                        Value = FatEntity.Convert(entity, DataSerializer)
                    };
                }
            }
        }
        /// <see cref="ITableStorageProvider.Update{T}"/>
        public void Upsert<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
        {
            var list = entities.ToArray();

            lock (syncRoot)
            {
                // deleting all existing entities
                foreach (var g in list.GroupBy(e => e.PartitionKey))
                {
                    Delete<T>(tableName, g.Key, g.Select(e => e.RowKey));
                }

                // inserting all entities
                Insert(tableName, list);
            }
        }

        /// <see cref="ITableStorageProvider.Delete{T}(string,string,IEnumerable{string})"/>
        public void Delete<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys)
        {
            lock (syncRoot)
            {
                List<MockTableEntry> entries;
                if (!tables.TryGetValue(tableName, out entries))
                {
                    return;
                }

                var keys = new HashSet<string>(rowKeys);
                entries.RemoveAll(entry => entry.PartitionKey == partitionKey && keys.Contains(entry.RowKey));
            }
        }

        /// <remarks></remarks>
        public void Delete<T>(string tableName, IEnumerable<CloudEntity<T>> entities, bool force)
        {
            lock (syncRoot)
            {
                List<MockTableEntry> entries;
                if (!tables.TryGetValue(tableName, out entries))
                {
                    return;
                }

                var entityList = entities.ToList();

                // verify valid data BEFORE deleting them
                if (entityList.Join(entries, ToId, ToId, (u, v) => force || u.ETag == null || u.ETag == v.ETag).Any(c => !c))
                {
                    throw new DataServiceRequestException("DELETE: etag conflict.");
                }

                // ok, we can delete safely now
                entries.RemoveAll(entry => entityList.Any(entity => entity.PartitionKey == entry.PartitionKey && entity.RowKey == entry.RowKey));
            }
        }

        static Tuple<string, string> ToId<T>(CloudEntity<T> entity)
        {
            return Tuple.Create(entity.PartitionKey, entity.RowKey);
        }
        static Tuple<string, string> ToId(MockTableEntry entry)
        {
            return Tuple.Create(entry.PartitionKey, entry.RowKey);
        }

        [DataContract]
        internal class MockTableEntry
        {
            [DataMember]
            public string PartitionKey { get; set; }

            [DataMember]
            public string RowKey { get; set; }

            [DataMember]
            public string ETag { get; set; }

            [DataMember]
            public FatEntity Value { get; set; }

            public CloudEntity<T> ToCloudEntity<T>(Cloud.Storage.Shared.IDataSerializer serializer)
            {
                return FatEntity.Convert<T>(Value, serializer, ETag);
            }
        }
    }
}