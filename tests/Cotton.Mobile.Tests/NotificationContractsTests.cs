using System.Collections.Generic;
using System.Linq;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class NotificationContractsTests
    {
        [Fact]
        public void Notification_channel_catalog_defines_stable_android_channels()
        {
            IReadOnlyList<CottonNotificationChannelSnapshot> channels =
                CottonNotificationChannelCatalog.All;

            Assert.Equal(4, channels.Count);
            Assert.Equal(
                new[]
                {
                    CottonNotificationChannelKind.Transfers,
                    CottonNotificationChannelKind.Backup,
                    CottonNotificationChannelKind.Shares,
                    CottonNotificationChannelKind.Security,
                },
                channels.Select(channel => channel.Kind).ToArray());
            Assert.Equal(
                new[] { "cotton.transfers", "cotton.backup", "cotton.shares", "cotton.security" },
                channels.Select(channel => channel.Id).ToArray());
            Assert.Equal(channels.Count, channels.Select(channel => channel.Id).Distinct().Count());
            Assert.Equal("Transfers", CottonNotificationChannelCatalog.Get(CottonNotificationChannelKind.Transfers).Name);
            Assert.Equal("Backup", CottonNotificationChannelCatalog.Get(CottonNotificationChannelKind.Backup).Name);
            Assert.Equal("Shares", CottonNotificationChannelCatalog.Get(CottonNotificationChannelKind.Shares).Name);
            Assert.Equal("Security", CottonNotificationChannelCatalog.Get(CottonNotificationChannelKind.Security).Name);
        }

        [Fact]
        public void Default_notification_settings_enable_only_useful_early_channels()
        {
            CottonNotificationSettings settings = CottonNotificationSettings.Default;

            Assert.True(settings.IsEnabled(CottonNotificationChannelKind.Transfers));
            Assert.True(settings.IsEnabled(CottonNotificationChannelKind.Backup));
            Assert.False(settings.IsEnabled(CottonNotificationChannelKind.Shares));
            Assert.True(settings.IsEnabled(CottonNotificationChannelKind.Security));
            Assert.Equal(3, settings.EnabledChannelCount);
        }

        [Fact]
        public void Notification_settings_channel_toggles_are_immutable()
        {
            CottonNotificationSettings original = CottonNotificationSettings.Default;
            CottonNotificationSettings changed = original
                .WithChannelEnabled(CottonNotificationChannelKind.Shares, true)
                .WithChannelEnabled(CottonNotificationChannelKind.Backup, false);

            Assert.False(original.IsEnabled(CottonNotificationChannelKind.Shares));
            Assert.True(original.IsEnabled(CottonNotificationChannelKind.Backup));

            Assert.True(changed.IsEnabled(CottonNotificationChannelKind.Shares));
            Assert.False(changed.IsEnabled(CottonNotificationChannelKind.Backup));
            Assert.Equal(3, changed.EnabledChannelCount);
        }

        [Theory]
        [InlineData(CottonNotificationPermissionState.NotRequired, false)]
        [InlineData(CottonNotificationPermissionState.NotRequested, true)]
        [InlineData(CottonNotificationPermissionState.Allowed, false)]
        [InlineData(CottonNotificationPermissionState.Denied, false)]
        [InlineData(CottonNotificationPermissionState.Unavailable, false)]
        public void Notification_permission_is_requested_only_when_user_has_enabled_channels(
            CottonNotificationPermissionState permissionState,
            bool shouldRequest)
        {
            CottonNotificationSettings settings = CottonNotificationSettings.Default;
            CottonNotificationSettings allOff = settings
                .WithChannelEnabled(CottonNotificationChannelKind.Transfers, false)
                .WithChannelEnabled(CottonNotificationChannelKind.Backup, false)
                .WithChannelEnabled(CottonNotificationChannelKind.Shares, false)
                .WithChannelEnabled(CottonNotificationChannelKind.Security, false);

            Assert.Equal(shouldRequest, settings.ShouldRequestPermission(permissionState));
            Assert.False(allOff.ShouldRequestPermission(permissionState));
        }

        [Theory]
        [InlineData(CottonNotificationPermissionState.NotRequired, "Allowed", "", true, false, false)]
        [InlineData(CottonNotificationPermissionState.NotRequested, "Not requested", "Allow", false, false, false)]
        [InlineData(CottonNotificationPermissionState.Allowed, "Allowed", "", true, false, false)]
        [InlineData(CottonNotificationPermissionState.Denied, "Denied", "Settings", false, true, true)]
        [InlineData(CottonNotificationPermissionState.Unavailable, "Unavailable", "", false, true, false)]
        public void Notification_permission_display_state_keeps_status_and_action_explicit(
            CottonNotificationPermissionState permissionState,
            string statusText,
            string actionText,
            bool canSend,
            bool needsAttention,
            bool shouldOpenSettings)
        {
            CottonNotificationPermissionDisplayState display =
                CottonNotificationPermissionDisplayState.Create(CottonNotificationSettings.Default, permissionState);

            Assert.Equal("Notifications", display.Title);
            Assert.Equal(statusText, display.StatusText);
            Assert.Equal(actionText, display.ActionText);
            Assert.Equal(!string.IsNullOrWhiteSpace(actionText), display.IsActionVisible);
            Assert.Equal(canSend, display.CanSendNotifications);
            Assert.Equal(needsAttention, display.NeedsAttention);
            Assert.Equal(shouldOpenSettings, display.ShouldOpenSettings);
            Assert.False(string.IsNullOrWhiteSpace(display.DetailText));
        }

        [Fact]
        public void Notification_permission_display_does_not_prompt_when_all_categories_are_off()
        {
            CottonNotificationSettings allOff = CottonNotificationSettings.Default
                .WithChannelEnabled(CottonNotificationChannelKind.Transfers, false)
                .WithChannelEnabled(CottonNotificationChannelKind.Backup, false)
                .WithChannelEnabled(CottonNotificationChannelKind.Shares, false)
                .WithChannelEnabled(CottonNotificationChannelKind.Security, false);

            CottonNotificationPermissionDisplayState display =
                CottonNotificationPermissionDisplayState.Create(
                    allOff,
                    CottonNotificationPermissionState.NotRequested);

            Assert.Equal("Off", display.StatusText);
            Assert.False(display.CanSendNotifications);
            Assert.False(display.IsActionVisible);
            Assert.False(display.NeedsAttention);
        }
    }
}
