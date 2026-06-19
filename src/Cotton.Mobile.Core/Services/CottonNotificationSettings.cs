using System;
using System.Collections.Generic;
using System.Linq;

namespace Cotton.Mobile.Services
{
    public sealed class CottonNotificationSettings
    {
        private readonly IReadOnlyDictionary<CottonNotificationChannelKind, bool> _channelEnabled;

        public static CottonNotificationSettings Default { get; } =
            new CottonNotificationSettings(
                CottonNotificationChannelCatalog.All.ToDictionary(
                    channel => channel.Kind,
                    channel => channel.DefaultEnabled));

        public CottonNotificationSettings(
            IReadOnlyDictionary<CottonNotificationChannelKind, bool> channelEnabled)
        {
            if (channelEnabled is null)
            {
                throw new ArgumentNullException(nameof(channelEnabled));
            }

            _channelEnabled = CottonNotificationChannelCatalog.All.ToDictionary(
                channel => channel.Kind,
                channel => channelEnabled.TryGetValue(channel.Kind, out bool isEnabled)
                    ? isEnabled
                    : channel.DefaultEnabled);
        }

        public int EnabledChannelCount => _channelEnabled.Count(pair => pair.Value);

        public bool HasEnabledChannels => EnabledChannelCount > 0;

        public bool IsEnabled(CottonNotificationChannelKind kind)
        {
            return _channelEnabled.TryGetValue(kind, out bool isEnabled) && isEnabled;
        }

        public IReadOnlyDictionary<CottonNotificationChannelKind, bool> ChannelEnabled => _channelEnabled;

        public CottonNotificationSettings WithChannelEnabled(
            CottonNotificationChannelKind kind,
            bool isEnabled)
        {
            Dictionary<CottonNotificationChannelKind, bool> values = _channelEnabled.ToDictionary(
                pair => pair.Key,
                pair => pair.Value);
            values[kind] = isEnabled;
            return new CottonNotificationSettings(values);
        }

        public bool ShouldRequestPermission(CottonNotificationPermissionState permissionState)
        {
            return permissionState == CottonNotificationPermissionState.NotRequested
                && HasEnabledChannels;
        }
    }
}
