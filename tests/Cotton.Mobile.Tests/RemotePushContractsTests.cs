using System.Linq;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RemotePushContractsTests
    {
        [Fact]
        public void Android_closed_testing_path_uses_fcm_and_keeps_delivery_gap_explicit()
        {
            CottonRemotePushCapabilitySnapshot capability =
                CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend;

            Assert.Equal(CottonRemotePushProviderKind.FirebaseCloudMessaging, capability.Provider);
            Assert.Equal(CottonRemotePushMobilePlatform.Android, capability.Platform);
            Assert.True(capability.RequiresAndroidPostNotificationsPermissionForVisibleAlerts);
            Assert.True(capability.CanRegisterDeviceToken);
            Assert.False(capability.CanDeliverRemotePush);
            Assert.Contains(
                CottonRemotePushServerCapabilityKind.DeviceTokenRegistrationEndpoint,
                capability.AvailableServerCapabilities);
            Assert.Contains(
                CottonRemotePushServerCapabilityKind.DeviceTokenRefreshUpsert,
                capability.AvailableServerCapabilities);
            Assert.Contains(
                CottonRemotePushServerCapabilityKind.LogoutTokenRevocation,
                capability.AvailableServerCapabilities);
            Assert.Contains(
                CottonRemotePushServerCapabilityKind.ServerEventCategories,
                capability.AvailableServerCapabilities);
            Assert.Contains(
                CottonRemotePushServerCapabilityKind.PushPayloadPrivacyFiltering,
                capability.AvailableServerCapabilities);
            Assert.DoesNotContain(
                CottonRemotePushServerCapabilityKind.DeviceTokenRegistrationEndpoint,
                capability.MissingServerCapabilities);
            Assert.DoesNotContain(
                CottonRemotePushServerCapabilityKind.ServerEventCategories,
                capability.MissingServerCapabilities);
            Assert.DoesNotContain(
                CottonRemotePushServerCapabilityKind.PushPayloadPrivacyFiltering,
                capability.MissingServerCapabilities);
            Assert.Contains(
                CottonRemotePushServerCapabilityKind.StaleTokenCleanup,
                capability.MissingServerCapabilities);
            Assert.Contains(
                CottonRemotePushServerCapabilityKind.UserNotificationPreferences,
                capability.MissingServerCapabilities);
        }

        [Fact]
        public void Remote_push_event_categories_map_only_to_share_and_security_channels()
        {
            CottonRemotePushCapabilitySnapshot capability =
                CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend;

            Assert.Equal(
                new[]
                {
                    CottonRemotePushEventCategory.SharedFile,
                    CottonRemotePushEventCategory.AccessRequest,
                    CottonRemotePushEventCategory.CommentMention,
                    CottonRemotePushEventCategory.SecuritySession,
                },
                capability.EventCategories.Select(category => category.Category).ToArray());
            Assert.All(
                capability.EventCategories.Where(category =>
                    category.Category is CottonRemotePushEventCategory.SharedFile
                        or CottonRemotePushEventCategory.AccessRequest
                        or CottonRemotePushEventCategory.CommentMention),
                category => Assert.Equal(CottonNotificationChannelKind.Shares, category.ChannelKind));
            Assert.Equal(
                CottonNotificationChannelKind.Security,
                capability.EventCategories.Single(category =>
                    category.Category == CottonRemotePushEventCategory.SecuritySession).ChannelKind);
            Assert.DoesNotContain(
                capability.EventCategories,
                category => category.ChannelKind is CottonNotificationChannelKind.Transfers
                    or CottonNotificationChannelKind.Backup);
        }

        [Fact]
        public void Remote_push_defaults_allow_security_but_not_collaboration_noise()
        {
            CottonNotificationSettings settings = CottonNotificationSettings.Default;
            CottonRemotePushCapabilitySnapshot capability =
                CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend;

            Assert.False(capability.EventCategories.Single(category =>
                category.Category == CottonRemotePushEventCategory.SharedFile).IsEnabledByDefault(settings));
            Assert.False(capability.EventCategories.Single(category =>
                category.Category == CottonRemotePushEventCategory.AccessRequest).IsEnabledByDefault(settings));
            Assert.False(capability.EventCategories.Single(category =>
                category.Category == CottonRemotePushEventCategory.CommentMention).IsEnabledByDefault(settings));
            Assert.True(capability.EventCategories.Single(category =>
                category.Category == CottonRemotePushEventCategory.SecuritySession).IsEnabledByDefault(settings));
        }

        [Fact]
        public void Remote_push_payload_policy_requires_generic_visible_payloads()
        {
            CottonRemotePushPayloadPrivacyPolicy policy =
                CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend.PayloadPrivacyPolicy;

            Assert.True(policy.RequiresNotificationId);
            Assert.True(policy.RequiresEventCategory);
            Assert.False(policy.AllowsFileNames);
            Assert.False(policy.AllowsFolderNames);
            Assert.False(policy.AllowsAccountIdentifiers);
            Assert.False(policy.AllowsPublicShareTokens);
        }
    }
}
