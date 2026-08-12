using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncTraversalGuardTests
    {
        [Fact]
        public void Duplicate_container_is_not_entered_twice()
        {
            CottonSyncTraversalGuard<Guid> guard = new();
            Guid identifier = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            Assert.True(guard.TryEnterContainer(identifier, 0));
            Assert.False(guard.TryEnterContainer(identifier, 0));
        }

        [Fact]
        public void Excessive_depth_is_rejected()
        {
            CottonSyncTraversalGuard<string> guard = new(maximumDepth: 1, maximumItemCount: 10);

            Assert.Throws<InvalidDataException>(() => guard.TryEnterContainer("too-deep", 2));
        }

        [Fact]
        public void Excessive_item_count_is_rejected()
        {
            CottonSyncTraversalGuard<string> guard = new(maximumDepth: 1, maximumItemCount: 1);
            guard.RecordItem();

            Assert.Throws<InvalidDataException>(guard.RecordItem);
        }
    }
}
