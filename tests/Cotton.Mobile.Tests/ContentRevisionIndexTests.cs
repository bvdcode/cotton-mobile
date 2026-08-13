using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ContentRevisionIndexTests
    {
        [Fact]
        public void MatchingGenerationReturnsCachedHash()
        {
            CottonContentRevisionIndexSnapshot index = CreateIndex(generation: 12);

            bool found = index.TryGetContentHash("content://media/1", 12, out string? contentHash);

            Assert.True(found);
            Assert.Equal(TestContentHashes.First, contentHash);
        }

        [Fact]
        public void ChangedGenerationDoesNotReturnCachedHash()
        {
            CottonContentRevisionIndexSnapshot index = CreateIndex(generation: 12);

            bool found = index.TryGetContentHash("content://media/1", 13, out string? contentHash);

            Assert.False(found);
            Assert.Null(contentHash);
        }

        [Fact]
        public void EquivalentIndexesIgnoreInputOrder()
        {
            CottonContentRevisionSnapshot first = new("content://media/1", 12, TestContentHashes.First);
            CottonContentRevisionSnapshot second = new("content://media/2", 24, TestContentHashes.Second);
            CottonContentRevisionIndexSnapshot left = new("version-1", [first, second]);
            CottonContentRevisionIndexSnapshot right = new("version-1", [second, first]);

            Assert.True(left.HasSameContentAs(right));
        }

        [Fact]
        public void DuplicateSourceIdsAreRejected()
        {
            CottonContentRevisionSnapshot first = new("content://media/1", 12, TestContentHashes.First);
            CottonContentRevisionSnapshot second = new("content://media/1", 24, TestContentHashes.Second);

            Assert.Throws<ArgumentException>(() =>
                new CottonContentRevisionIndexSnapshot("version-1", [first, second]));
        }

        private static CottonContentRevisionIndexSnapshot CreateIndex(long generation)
        {
            return new CottonContentRevisionIndexSnapshot(
                "version-1",
                [new CottonContentRevisionSnapshot("content://media/1", generation, TestContentHashes.First)]);
        }
    }
}
