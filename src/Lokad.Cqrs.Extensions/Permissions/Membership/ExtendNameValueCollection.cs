using System.Collections.Generic;
using System.Collections.Specialized;

namespace Lokad.Cqrs.Extensions.Permissions.Membership
{
    public static class ExtendNameValueCollection
    {
        public static int GetInt(this NameValueCollection collection, string key)
        {
            return GetOrThrow<int>(collection, key, int.TryParse);
        }

        public static int GetInt(this NameValueCollection collection, string key, int defaultValue)
        {
            return GetOrDefault(collection, key, int.TryParse, defaultValue);
        }

        public static bool GetBool(this NameValueCollection collection, string key)
        {
            return GetOrThrow<bool>(collection, key, bool.TryParse);
        }

        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue)
        {
            return GetOrDefault(collection, key, bool.TryParse, defaultValue);
        }

        public static string GetString(this NameValueCollection collection, string key)
        {
            return GetOrThrow<string>(collection, key, StringConversion);
        }

        public static string GetString(this NameValueCollection collection, string key, string defaultValue)
        {
            return GetOrDefault(collection, key, StringConversion, defaultValue);
        }

        private static T GetOrDefault<T>(this NameValueCollection collection, string key, Conversion<T> conversion,
                                         T defaultValue)
        {
            T value;
            return TryGet(collection, key, conversion, out value) ? value : defaultValue;
        }

        private static T GetOrThrow<T>(this NameValueCollection collection, string key, Conversion<T> conversion)
        {
            T value;
            bool hasValue = TryGet(collection, key, conversion, out value);
            if (hasValue)
                return value;
            throw new KeyNotFoundException(key);
        }

        private static bool TryGet<TValue>(this NameValueCollection collection, string key,
                                           Conversion<TValue> conversion, out TValue value)
        {
            value = default(TValue);
            string s = collection[key];
            if (string.IsNullOrWhiteSpace(s))
                return false;
            return conversion(key, out value);
        }

        private static bool StringConversion(string stringValue, out string output)
        {
            output = stringValue;
            if (string.IsNullOrWhiteSpace(stringValue))
                return false;
            return true;
        }

        #region Nested type: Conversion

        private delegate bool Conversion<TValue>(string stringValue, out TValue output);

        #endregion
    }
}