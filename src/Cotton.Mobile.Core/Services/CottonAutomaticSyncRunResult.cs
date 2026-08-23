// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncRunResult
    {
        public CottonAutomaticSyncRunResult(
            IEnumerable<Guid> succeededRootIds,
            IEnumerable<CottonAutomaticSyncFailure> failures)
        {
            ArgumentNullException.ThrowIfNull(succeededRootIds);
            ArgumentNullException.ThrowIfNull(failures);

            HashSet<Guid> succeeded = CreateRootIdSet(succeededRootIds, nameof(succeededRootIds));
            Dictionary<Guid, CottonAutomaticSyncFailure> failed = CreateFailureMap(failures);
            if (succeeded.Overlaps(failed.Keys))
            {
                throw new ArgumentException("A sync root cannot be both successful and failed.", nameof(failures));
            }

            SucceededRootIds = [.. succeeded.Order()];
            Failures = [.. failed.Values.OrderBy(failure => failure.RootId)];
        }

        public static CottonAutomaticSyncRunResult Empty { get; } = new([], []);

        public IReadOnlyList<Guid> SucceededRootIds { get; }

        public IReadOnlyList<CottonAutomaticSyncFailure> Failures { get; }

        public IReadOnlyList<Guid> FailedRootIds => [.. Failures.Select(failure => failure.RootId)];

        public IReadOnlyList<Guid> RetryableRootIds =>
            [.. Failures.Where(failure => CottonAutomaticSyncRetryPolicy.IsRetryable(failure.Kind))
                .Select(failure => failure.RootId)];

        public bool HasFailures => FailedRootIds.Count > 0;

        public CottonAutomaticSyncRunResult Merge(CottonAutomaticSyncRunResult next)
        {
            ArgumentNullException.ThrowIfNull(next);

            HashSet<Guid> succeeded = [.. SucceededRootIds];
            Dictionary<Guid, CottonAutomaticSyncFailure> failed = Failures.ToDictionary(
                failure => failure.RootId);
            foreach (Guid rootId in next.SucceededRootIds)
            {
                failed.Remove(rootId);
                succeeded.Add(rootId);
            }

            foreach (CottonAutomaticSyncFailure failure in next.Failures)
            {
                succeeded.Remove(failure.RootId);
                failed[failure.RootId] = failure;
            }

            return new CottonAutomaticSyncRunResult(succeeded, failed.Values);
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

        private static Dictionary<Guid, CottonAutomaticSyncFailure> CreateFailureMap(
            IEnumerable<CottonAutomaticSyncFailure> failures)
        {
            Dictionary<Guid, CottonAutomaticSyncFailure> result = [];
            foreach (CottonAutomaticSyncFailure failure in failures)
            {
                ArgumentNullException.ThrowIfNull(failure);
                if (!result.TryAdd(failure.RootId, failure))
                {
                    throw new ArgumentException("A sync root failure cannot be repeated.", nameof(failures));
                }
            }

            return result;
        }
    }
}
