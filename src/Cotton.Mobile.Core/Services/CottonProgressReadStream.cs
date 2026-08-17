// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonProgressReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly IProgress<long> _progress;
        private readonly bool _leaveOpen;
        private readonly long _initialPosition;
        private long _reportedBytes;

        public CottonProgressReadStream(
            Stream inner,
            IProgress<long> progress,
            bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(progress);
            if (!inner.CanRead)
            {
                throw new ArgumentException("Progress stream requires a readable stream.", nameof(inner));
            }

            _inner = inner;
            _progress = progress;
            _leaveOpen = leaveOpen;
            _initialPosition = inner.CanSeek ? inner.Position : 0;
        }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return RecordRead(_inner.Read(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            return RecordRead(_inner.Read(buffer));
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int bytesRead = await _inner
                .ReadAsync(buffer.AsMemory(offset, count), cancellationToken)
                .ConfigureAwait(false);
            return RecordRead(bytesRead);
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            int bytesRead = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            return RecordRead(bytesRead);
        }

        public override int ReadByte()
        {
            int value = _inner.ReadByte();
            if (value >= 0)
            {
                RecordRead(1);
            }

            return value;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Progress read stream is read-only.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Progress read stream is read-only.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveOpen)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        private int RecordRead(int bytesRead)
        {
            if (bytesRead <= 0)
            {
                return bytesRead;
            }

            long transferredBytes = _inner.CanSeek
                ? Math.Max(0, _inner.Position - _initialPosition)
                : _reportedBytes + bytesRead;
            if (transferredBytes > _reportedBytes)
            {
                _reportedBytes = transferredBytes;
                _progress.Report(transferredBytes);
            }

            return bytesRead;
        }
    }
}
