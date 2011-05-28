using EventStore;

using Lokad.Cqrs.Extensions.EventStore.Events;

namespace Lokad.Cqrs.Extensions.EventStore
{
    class PipelineHookSystemObserver : IPipelineHook
    {
        private readonly ISystemObserver observer;

        public PipelineHookSystemObserver(ISystemObserver observer)
        {
            this.observer = observer;
        }

        public Commit Select(Commit committed)
        {
            var @event = new CommitSelected(committed.StreamId, committed.StreamRevision, committed.Events.Count);

            observer.Notify(@event);

            return committed;
        }

        public bool PreCommit(Commit attempt)
        {
            var @event = new PreCommit(attempt.StreamId, attempt.StreamRevision, attempt.Events.Count);

            observer.Notify(@event);

            return true;
        }

        public void PostCommit(Commit committed)
        {
            var @event = new PostCommit(committed.StreamId, committed.StreamRevision, committed.Events.Count);

            observer.Notify(@event);
        }
    }
}