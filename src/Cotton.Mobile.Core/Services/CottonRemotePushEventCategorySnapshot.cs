// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System;

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushEventCategorySnapshot
    {
        public CottonRemotePushEventCategorySnapshot(
            CottonRemotePushEventCategory category,
            CottonNotificationChannelKind channelKind,
            bool defaultEnabled,
            bool requiresServerNotificationRow)
        {
            if (!Enum.IsDefined(category))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(category),
                    "Remote push event category is not supported.");
            }

            if (!Enum.IsDefined(channelKind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(channelKind),
                    "Notification channel kind is not supported.");
            }

            Category = category;
            ChannelKind = channelKind;
            DefaultEnabled = defaultEnabled;
            RequiresServerNotificationRow = requiresServerNotificationRow;
        }

        public CottonRemotePushEventCategory Category { get; }

        public CottonNotificationChannelKind ChannelKind { get; }

        public bool DefaultEnabled { get; }

        public bool RequiresServerNotificationRow { get; }

        public bool IsEnabledByDefault(CottonNotificationSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            return settings.IsEnabled(ChannelKind) && DefaultEnabled;
        }
    }
}
