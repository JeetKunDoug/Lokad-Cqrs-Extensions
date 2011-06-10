using System;
using System.Linq;

using Lokad.Cqrs;

namespace Context
{
    public static class ExtendImmutableEnvelope
    {
        public static Guid GetGuid(this ImmutableEnvelope envelope, string key)
        {
            return Get(envelope, key, s =>
            {
                Guid result;
                return Guid.TryParse(s, out result) == false ? Guid.Empty : result;
            });
        }

        public static string GetString(this ImmutableEnvelope envelope, string key)
        {
            return GetString(envelope, key, "");
        }

        public static string GetString(this ImmutableEnvelope envelope, string key, string defaultValue)
        {
            return Get(envelope, key, s => string.IsNullOrWhiteSpace(s) ? defaultValue : s);
        }

        private static T Get<T>(this ImmutableEnvelope envelope, string key, Func<string, T> convert)
        {
            ImmutableAttribute attribute =
                envelope.GetAllAttributes().Where(a => a.Key == key).FirstOrDefault();

            return convert(attribute == null ? string.Empty : attribute.Value);
        }
    }
}