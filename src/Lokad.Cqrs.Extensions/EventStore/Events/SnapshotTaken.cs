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
    }

}