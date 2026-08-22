using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ProgressReadStreamTests
    {
        [Fact]
        public async Task ReadReportsTransferredBytesWhileKeepingInnerStreamOpen()
        {
            byte[] content = [1, 2, 3, 4];
            using MemoryStream inner = new(content);
            TaskCompletionSource<long> reported = new(TaskCreationOptions.RunContinuationsAsynchronously);
            IProgress<long> progress = new Progress<long>(value => reported.TrySetResult(value));
            await using (CottonProgressReadStream stream = new(inner, progress, leaveOpen: true))
            {
                byte[] buffer = new byte[3];

                int bytesRead = await stream.ReadAsync(buffer);

                Assert.Equal(3, bytesRead);
                Assert.Equal(3, await reported.Task.WaitAsync(TimeSpan.FromSeconds(1)));
            }

            Assert.True(inner.CanRead);
        }
    }
}
