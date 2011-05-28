using System;

namespace Lokad.Cqrs.Extensions.EventStore.Events
{
    public class SnapshotTaken : ISystemEvent
    {
        public SnapshotTaken(Guid streamId, int revision)
        {
            StreamId = streamId;
            Revision = revision;
        }

        public Guid StreamId { get; private set; }
        public int Revision { get; private set; }

        public override string ToString()
        {
            return string.Format("Took snapshot of stream [{0}] at revison {1}", StreamId, Revision);
        }
    }

}