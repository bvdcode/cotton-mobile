// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncRunResult
    {
        public CottonAutomaticSyncRunResult(
            IEnumerable<Guid> succeededRootIds,
            IEnumerable<Guid> failedRootIds)
        {
            ArgumentNullException.ThrowIfNull(succeededRootIds);
            ArgumentNullException.ThrowIfNull(failedRootIds);

            HashSet<Guid> succeeded = CreateRootIdSet(succeededRootIds, nameof(succeededRootIds));
            HashSet<Guid> failed = CreateRootIdSet(failedRootIds, nameof(failedRootIds));
            if (succeeded.Overlaps(failed))
            {
                throw new ArgumentException("A sync root cannot be both successful and failed.", nameof(failedRootIds));
            }

            SucceededRootIds = [.. succeeded.Order()];
            FailedRootIds = [.. failed.Order()];
        }

        public static CottonAutomaticSyncRunResult Empty { get; } = new([], []);

        public IReadOnlyList<Guid> SucceededRootIds { get; }

        public IReadOnlyList<Guid> FailedRootIds { get; }

        public bool HasFailures => FailedRootIds.Count > 0;

        public CottonAutomaticSyncRunResult Merge(CottonAutomaticSyncRunResult next)
        {
            ArgumentNullException.ThrowIfNull(next);

            HashSet<Guid> succeeded = [.. SucceededRootIds];
            HashSet<Guid> failed = [.. FailedRootIds];
            foreach (Guid rootId in next.SucceededRootIds)
            {
                failed.Remove(rootId);
                succeeded.Add(rootId);
            }

            foreach (Guid rootId in next.FailedRootIds)
            {
                succeeded.Remove(rootId);
                failed.Add(rootId);
            }

            return new CottonAutomaticSyncRunResult(succeeded, failed);
        }

        private static HashSet<Guid> CreateRootIdSet(IEnumerable<Guid> rootIds, string parameterName)
        {
            HashSet<Guid> result = [];
            foreach (Guid rootId in rootIds)
            {
                if (rootId == Guid.Empty)
                {
                    throw new ArgumentException("Sync root ids cannot be empty.", parameterName);
                }

                result.Add(rootId);
            }

            return result;
        }
    }
}
