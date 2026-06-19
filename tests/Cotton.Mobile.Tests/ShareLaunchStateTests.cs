using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ShareLaunchStateTests
    {
        [Fact]
        public void NotifyShareStaged_records_pending_launch_and_raises_event()
        {
            var state = new CottonShareLaunchState();
            int eventCount = 0;
            state.ShareStaged += (_, _) => eventCount++;

            state.NotifyShareStaged();

            Assert.Equal(1, eventCount);
            Assert.Equal(1, state.PendingShareLaunchCount);
        }

        [Fact]
        public void TryConsumePendingShareLaunch_consumes_one_pending_launch_at_a_time()
        {
            var state = new CottonShareLaunchState();
            state.NotifyShareStaged();
            state.NotifyShareStaged();

            Assert.True(state.TryConsumePendingShareLaunch());
            Assert.Equal(1, state.PendingShareLaunchCount);
            Assert.True(state.TryConsumePendingShareLaunch());
            Assert.Equal(0, state.PendingShareLaunchCount);
            Assert.False(state.TryConsumePendingShareLaunch());
        }

        [Fact]
        public void Pending_launch_count_survives_new_state_instance_when_file_backed()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-share-launch-state-tests",
                Guid.NewGuid().ToString("N"));
            try
            {
                var firstState = new CottonShareLaunchState(new FixedShareIntakePathProvider(directory));
                firstState.NotifyShareStaged();
                firstState.NotifyShareStaged();

                var secondState = new CottonShareLaunchState(new FixedShareIntakePathProvider(directory));

                Assert.Equal(2, secondState.PendingShareLaunchCount);
                Assert.True(secondState.TryConsumePendingShareLaunch());
                Assert.Equal(1, firstState.PendingShareLaunchCount);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }

        private class FixedShareIntakePathProvider : ICottonShareIntakePathProvider
        {
            private readonly string _directory;

            public FixedShareIntakePathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateShareIntakeDirectory()
            {
                return _directory;
            }
        }
    }
}
