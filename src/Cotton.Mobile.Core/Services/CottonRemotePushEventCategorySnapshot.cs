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
