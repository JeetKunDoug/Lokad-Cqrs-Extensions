using System.IO;
using System.Runtime.Serialization.Json;

using Lokad.Cqrs.Feature.AtomicStorage;

namespace Lokad.Cqrs.Extensions
{
    public class AtomicStorageSerializerWithJson : IAtomicStorageSerializer
    {
        #region Implementation of IAtomicStorageSerializer

        public void Serialize<TView>(TView view, Stream stream)
        {
            var serializer = new DataContractJsonSerializer(typeof(TView));
            serializer.WriteObject(stream, view);
        }

        public TView Deserialize<TView>(Stream stream)
        {
            var serializer = new DataContractJsonSerializer(typeof(TView));
            return (TView)serializer.ReadObject(stream);
        }

        #endregion
    }
}