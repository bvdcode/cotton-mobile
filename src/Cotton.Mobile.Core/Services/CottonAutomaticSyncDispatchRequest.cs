// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonAutomaticSyncDispatchRequest
    {
        private CottonAutomaticSyncDispatchRequest(
            CottonAutomaticSyncTrigger? trigger,
            IReadOnlyList<Guid> rootIds)
        {
            Trigger = trigger;
            RootIds = rootIds;
        }

        public CottonAutomaticSyncTrigger? Trigger { get; }

        public IReadOnlyList<Guid> RootIds { get; }

        public static CottonAutomaticSyncDispatchRequest ForTrigger(
            CottonAutomaticSyncTrigger trigger)
        {
            if (!Enum.IsDefined(trigger))
            {
                throw new ArgumentOutOfRangeException(nameof(trigger), "Automatic sync trigger is not supported.");
            }

            return new CottonAutomaticSyncDispatchRequest(trigger, []);
        }

        public static CottonAutomaticSyncDispatchRequest ForRoots(
            IReadOnlyCollection<Guid> rootIds)
        {
            ArgumentNullException.ThrowIfNull(rootIds);
            Guid[] selectedRootIds = [.. rootIds.Distinct().Order()];
            if (selectedRootIds.Length == 0 || selectedRootIds.Contains(Guid.Empty))
            {
                throw new ArgumentException("Automatic sync root ids are required.", nameof(rootIds));
            }

            return new CottonAutomaticSyncDispatchRequest(null, selectedRootIds);
        }
    }
}
