using EventStore;

namespace Shared
{
    public class MyNullPipelineHook : IPipelineHook{
        public Commit Select(Commit committed)
        {
            return committed;
        }

        public bool PreCommit(Commit attempt)
        {
            return true;
        }

        public void PostCommit(Commit committed)
        {}
    }
}