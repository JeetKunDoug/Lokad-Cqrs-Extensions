using Lokad.Cloud.Storage;

namespace Lokad.Cqrs.Extensions.Storage
{
    public static class ExtendMaybe
    {
        public static T ValueOrDefault<T>(this Maybe<T> maybe)
        {
            return maybe.GetValue(default(T));
        }
    }
}