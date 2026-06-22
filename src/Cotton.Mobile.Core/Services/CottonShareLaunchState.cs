// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonShareLaunchState : ICottonShareLaunchState
    {
        private const string PendingLaunchFileName = "pending-launch.txt";

        private readonly Lock _gate = new();
        private readonly ICottonShareIntakePathProvider? _pathProvider;
        private int _pendingShareLaunchCount;

        public CottonShareLaunchState(ICottonShareIntakePathProvider? pathProvider = null)
        {
            _pathProvider = pathProvider;
        }

        public event EventHandler? ShareStaged;

        public int PendingShareLaunchCount
        {
            get
            {
                lock (_gate)
                {
                    return ReadPendingShareLaunchCount();
                }
            }
        }

        public void NotifyShareStaged()
        {
            lock (_gate)
            {
                WritePendingShareLaunchCount(ReadPendingShareLaunchCount() + 1);
            }

            ShareStaged?.Invoke(this, EventArgs.Empty);
        }

        public bool TryConsumePendingShareLaunch()
        {
            lock (_gate)
            {
                int pendingCount = ReadPendingShareLaunchCount();
                if (pendingCount == 0)
                {
                    return false;
                }

                WritePendingShareLaunchCount(pendingCount - 1);
                return true;
            }
        }

        private int ReadPendingShareLaunchCount()
        {
            if (_pathProvider is null)
            {
                return _pendingShareLaunchCount;
            }

            string filePath = CreatePendingLaunchFilePath();
            try
            {
                if (!File.Exists(filePath))
                {
                    return 0;
                }

                string value = File.ReadAllText(filePath).Trim();
                return int.TryParse(value, out int count) && count > 0 ? count : 0;
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                return 0;
            }
        }

        private void WritePendingShareLaunchCount(int count)
        {
            if (_pathProvider is null)
            {
                _pendingShareLaunchCount = Math.Max(0, count);
                return;
            }

            string filePath = CreatePendingLaunchFilePath();
            try
            {
                if (count <= 0)
                {
                    File.Delete(filePath);
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                string temporaryFilePath = $"{filePath}.{Guid.NewGuid():N}.tmp";
                File.WriteAllText(temporaryFilePath, count.ToString(System.Globalization.CultureInfo.InvariantCulture));
                File.Move(temporaryFilePath, filePath, overwrite: true);
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }

        private string CreatePendingLaunchFilePath()
        {
            return Path.Combine(_pathProvider!.CreateShareIntakeDirectory(), PendingLaunchFileName);
        }
    }
}
