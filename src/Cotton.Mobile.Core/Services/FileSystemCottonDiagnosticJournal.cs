// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonDiagnosticJournal : ICottonDiagnosticJournal, IDisposable
    {
        private const int MaximumFileSizeBytes = 256 * 1024;
        private const string CurrentFileName = "diagnostics.log";
        private const string PreviousFileName = "diagnostics.previous.log";

        private readonly Lock _gate = new();
        private readonly TimeProvider _timeProvider;
        private readonly string _currentPath;
        private readonly string _previousPath;
        private FileStream _stream;
        private StreamWriter _writer;
        private bool _disposed;

        public FileSystemCottonDiagnosticJournal(string directoryPath, TimeProvider timeProvider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
            ArgumentNullException.ThrowIfNull(timeProvider);

            Directory.CreateDirectory(directoryPath);
            _timeProvider = timeProvider;
            _currentPath = Path.Combine(directoryPath, CurrentFileName);
            _previousPath = Path.Combine(directoryPath, PreviousFileName);
            (_stream, _writer) = OpenWriter(_currentPath);
        }

        public void Write(
            LogLevel level,
            string category,
            EventId eventId,
            string message,
            Type? exceptionType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);
            ArgumentNullException.ThrowIfNull(message);

            string record = CottonDiagnosticRecordFormatter.Format(
                _timeProvider.GetUtcNow(),
                level,
                category,
                eventId,
                message,
                exceptionType);
            int recordSize = Encoding.UTF8.GetByteCount(record + Environment.NewLine);
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_stream.Length > 0 && _stream.Length + recordSize > MaximumFileSizeBytes)
                {
                    Rotate();
                }

                _writer.WriteLine(record);
                _writer.Flush();
            }
        }

        public IReadOnlyList<string> ReadAll()
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _writer.Flush();
                List<string> records = [];
                AppendRecords(_previousPath, records);
                AppendRecords(_currentPath, records);
                return records.AsReadOnly();
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _writer.Dispose();
                _stream.Dispose();
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        private void Rotate()
        {
            _writer.Dispose();
            _stream.Dispose();
            File.Move(_currentPath, _previousPath, overwrite: true);
            (_stream, _writer) = OpenWriter(_currentPath);
        }

        private static (FileStream Stream, StreamWriter Writer) OpenWriter(string path)
        {
            FileStream stream = new(
                path,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite);
            StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return (stream, writer);
        }

        private static void AppendRecords(string path, List<string> records)
        {
            if (!File.Exists(path))
            {
                return;
            }

            using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);
            using StreamReader reader = new(stream, Encoding.UTF8);
            while (reader.ReadLine() is { } record)
            {
                records.Add(record);
            }
        }
    }
}
