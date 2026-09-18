namespace Cotton.Mobile.Tests
{
    internal class ReadRequestRecordingMemoryStream(byte[] bytes) : MemoryStream(bytes, writable: false)
    {
        public int MaximumReadRequestLength { get; private set; }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            MaximumReadRequestLength = Math.Max(MaximumReadRequestLength, buffer.Length);
            return base.ReadAsync(buffer, cancellationToken);
        }
    }
}
