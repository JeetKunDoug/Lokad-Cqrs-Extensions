#region Copyright (c) 2011, EventDay Inc.
// Copyright (c) 2011, EventDay Inc.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the EventDay Inc. nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL EventDay Inc. BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections.Generic;
using System.IO;

using Lokad.Cloud.Storage;

namespace Lokad.Cqrs.Extensions.Storage
{
    class FileTableStore
    {
        private readonly List<string> tables;
        private readonly CloudFormatter formatter;
        private readonly string storePath;

        public FileTableStore()
        {
            tables = new List<string>();
            formatter = new CloudFormatter();

            storePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_data", "storage", "tablestorage");
            if (!Directory.Exists(storePath))
                Directory.CreateDirectory(storePath);

            foreach (var file in Directory.EnumerateFiles(storePath))
            {
                tables.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        public IEnumerable<string> Keys
        {
            get { return tables; }
        }

        public bool ContainsKey(string tableName)
        {
            return tables.Contains(tableName);
        }

        public void Add(string tableName, List<FileTableStorageProvider.MockTableEntry> entries)
        {
            var file = Path.Combine(storePath, tableName);
            if (File.Exists(file))
                return;

            using(var stream = File.OpenWrite(file))
            {
                formatter.Serialize(entries, stream);
            }

            tables.Add(tableName);
        }

        public void Remove(string tableName)
        {
            var file = Path.Combine(storePath, tableName);
            if (File.Exists(file))
                File.Delete(file);

            tables.Remove(tableName);
        }

        public IEnumerable<FileTableStorageProvider.MockTableEntry> this[string tableName]
        {
            get
            {
                var file = Path.Combine(storePath, tableName);
                using(var stream = File.OpenRead(file))
                {
                    var o = formatter.Deserialize(stream, typeof (List<FileTableStorageProvider.MockTableEntry>));
                    return (IEnumerable<FileTableStorageProvider.MockTableEntry>)o;
                }
            }
        }

        public bool TryGetValue(string tableName, out List<FileTableStorageProvider.MockTableEntry> entries)
        {
            entries = new List<FileTableStorageProvider.MockTableEntry>();
            var file = Path.Combine(storePath, tableName);
            if (!File.Exists(file))
                return false;

            using(var stream = File.OpenRead(file))
            {
                var o = formatter.Deserialize(stream, typeof(List<FileTableStorageProvider.MockTableEntry>));
                entries = (List<FileTableStorageProvider.MockTableEntry>)o;
                return true;
            }
        }

        public void InsertInto(string tableName, List<FileTableStorageProvider.MockTableEntry> entries)
        {
            var file = Path.Combine(storePath, tableName);
            if (!File.Exists(file))
                return;

            using (var stream = File.OpenWrite(file))
            {
                formatter.Serialize(entries, stream);
            }
        }
    }
}