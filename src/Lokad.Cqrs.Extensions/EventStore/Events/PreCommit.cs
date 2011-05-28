using System;

namespace Lokad.Cqrs.Extensions.EventStore.Events
{
    public class PreCommit : ISystemEvent
    {
        public PreCommit(Guid streamId, int streamRevision, int eventCount)
        {
            StreamId = streamId;
            StreamRevision = streamRevision;
            EventCount = eventCount;
        }

        public Guid StreamId { get; private set; }
        public int StreamRevision { get; private set; }
        public int EventCount { get; private set; }

        public override string ToString()
        {
            return string.Format("Committing event stream [{0}] rev {1}. [{2} events]", StreamId, StreamRevision,
                                 EventCount);
        }
    }
}