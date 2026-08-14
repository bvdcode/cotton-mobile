// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Collections.ObjectModel;

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncStatusesChangedEventArgs : EventArgs
    {
        public CottonAutomaticSyncStatusesChangedEventArgs(
            Uri instanceUri,
            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(statuses);

            InstanceUri = instanceUri;
            Statuses = new ReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>(
                new Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>(statuses));
        }

        public Uri InstanceUri { get; }

        public IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> Statuses { get; }
    }
}
