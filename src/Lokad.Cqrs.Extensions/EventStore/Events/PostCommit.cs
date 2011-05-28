using System;

namespace Lokad.Cqrs.Extensions.EventStore.Events
{
    public class PostCommit : ISystemEvent
    {
        public PostCommit(Guid streamId, int streamRevision, int eventCount)
        {
            StreamId = streamId;
            StreamRevision = streamRevision;
            EventCount = eventCount;
        }

        public Guid StreamId { get; private set; }
        public int StreamRevision { get; private set; }
        public int EventCount { get; private set; }
    }
}