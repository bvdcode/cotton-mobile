using System.Linq;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RemotePushContractsTests
    {
        [Fact]
        public void Android_closed_testing_path_uses_fcm_and_marks_backend_surface_complete()
        {
            CottonRemotePushCapabilitySnapshot capability =
                CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend;

            Assert.Equal(CottonRemotePushProviderKind.FirebaseCloudMessaging, capability.Provider);
            Assert.Equal(CottonRemotePushMobilePlatform.Android, capability.Platform);
            Assert.True(capability.RequiresAndroidPostNotificationsPermissionForVisibleAlerts);
            Assert.True(capability.CanRegisterDeviceToken);
            Assert.True(capability.CanDeliverRemotePush);
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
            Assert.Contains(
                CottonRemotePushServerCapabilityKind.StaleTokenCleanup,
                capability.AvailableServerCapabilities);
            Assert.Contains(
                CottonRemotePushServerCapabilityKind.UserNotificationPreferences,
                capability.AvailableServerCapabilities);
            Assert.Contains(
                CottonRemotePushServerCapabilityKind.ProviderDeliveryService,
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
            Assert.DoesNotContain(
                CottonRemotePushServerCapabilityKind.StaleTokenCleanup,
                capability.MissingServerCapabilities);
            Assert.DoesNotContain(
                CottonRemotePushServerCapabilityKind.UserNotificationPreferences,
                capability.MissingServerCapabilities);
            Assert.DoesNotContain(
                CottonRemotePushServerCapabilityKind.ProviderDeliveryService,
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
            CottonRemotePushPreferences preferences = CottonRemotePushPreferences.Default;

            Assert.False(capability.EventCategories.Single(category =>
                category.Category == CottonRemotePushEventCategory.SharedFile).IsEnabledByDefault(settings));
            Assert.False(capability.EventCategories.Single(category =>
                category.Category == CottonRemotePushEventCategory.AccessRequest).IsEnabledByDefault(settings));
            Assert.False(capability.EventCategories.Single(category =>
                category.Category == CottonRemotePushEventCategory.CommentMention).IsEnabledByDefault(settings));
            Assert.True(capability.EventCategories.Single(category =>
                category.Category == CottonRemotePushEventCategory.SecuritySession).IsEnabledByDefault(settings));
            Assert.False(preferences.SharedFile);
            Assert.False(preferences.AccessRequest);
            Assert.False(preferences.CommentMention);
            Assert.True(preferences.SecuritySession);
            Assert.Equal(1, preferences.EnabledCategoryCount);
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

        [Fact]
        public void Remote_push_data_keys_match_backend_fcm_payload()
        {
            Assert.Equal("notificationId", CottonRemotePushMessageDataKeys.NotificationId);
            Assert.Equal("eventCategory", CottonRemotePushMessageDataKeys.EventCategory);
            Assert.Equal("priority", CottonRemotePushMessageDataKeys.Priority);
            Assert.Equal("titleKey", CottonRemotePushMessageDataKeys.TitleKey);
            Assert.Equal("contentKey", CottonRemotePushMessageDataKeys.ContentKey);
        }

        [Theory]
        [InlineData(
            CottonRemotePushEventCategory.SharedFile,
            CottonLocalNotificationKind.RemoteSharedFile,
            CottonNotificationChannelKind.Shares,
            "Shared-file activity needs attention.")]
        [InlineData(
            CottonRemotePushEventCategory.AccessRequest,
            CottonLocalNotificationKind.RemoteAccessRequest,
            CottonNotificationChannelKind.Shares,
            "An access request needs attention.")]
        [InlineData(
            CottonRemotePushEventCategory.CommentMention,
            CottonLocalNotificationKind.RemoteCommentMention,
            CottonNotificationChannelKind.Shares,
            "A comment or mention needs attention.")]
        [InlineData(
            CottonRemotePushEventCategory.SecuritySession,
            CottonLocalNotificationKind.RemoteSecuritySession,
            CottonNotificationChannelKind.Security,
            "Security activity needs attention.")]
        public void Remote_push_notification_factory_builds_generic_visible_notifications(
            CottonRemotePushEventCategory category,
            CottonLocalNotificationKind expectedKind,
            CottonNotificationChannelKind expectedChannel,
            string expectedMessage)
        {
            IReadOnlyDictionary<string, string> data = CreateRemotePushData(category);

            CottonLocalNotificationSnapshot? notification =
                CottonRemotePushNotificationFactory.CreateVisibleNotification(data);

            Assert.NotNull(notification);
            Assert.Equal(expectedKind, notification.Kind);
            Assert.Equal(expectedChannel, notification.ChannelKind);
            Assert.Equal("Cotton Cloud", notification.Title);
            Assert.Equal(expectedMessage, notification.Message);
            Assert.True(notification.Id > 0);
        }

        [Fact]
        public void Remote_push_notification_factory_ignores_template_keys_for_visible_copy()
        {
            Dictionary<string, string> data = CreateRemotePushData(CottonRemotePushEventCategory.SharedFile);
            data[CottonRemotePushMessageDataKeys.TitleKey] = "private-file-name.pdf";
            data[CottonRemotePushMessageDataKeys.ContentKey] = "<account>";

            CottonLocalNotificationSnapshot? notification =
                CottonRemotePushNotificationFactory.CreateVisibleNotification(data);

            Assert.NotNull(notification);
            Assert.DoesNotContain("private-file-name", notification.Title);
            Assert.DoesNotContain("private-file-name", notification.Message);
            Assert.DoesNotContain("<account>", notification.Title);
            Assert.DoesNotContain("<account>", notification.Message);
            Assert.Equal("Shared-file activity needs attention.", notification.Message);
        }

        [Theory]
        [InlineData("", "SharedFile")]
        [InlineData("00000000-0000-0000-0000-000000000000", "SharedFile")]
        [InlineData("not-a-guid", "SharedFile")]
        [InlineData("11111111-2222-3333-4444-555555555555", "")]
        [InlineData("11111111-2222-3333-4444-555555555555", "Unknown")]
        [InlineData("11111111-2222-3333-4444-555555555555", "sharedfile")]
        public void Remote_push_notification_factory_rejects_non_displayable_payloads(
            string notificationId,
            string eventCategory)
        {
            Dictionary<string, string> data = new()
            {
                [CottonRemotePushMessageDataKeys.NotificationId] = notificationId,
                [CottonRemotePushMessageDataKeys.EventCategory] = eventCategory,
            };

            CottonLocalNotificationSnapshot? notification =
                CottonRemotePushNotificationFactory.CreateVisibleNotification(data);

            Assert.Null(notification);
        }

        [Fact]
        public void Remote_push_preferences_are_immutable_per_category()
        {
            CottonRemotePushPreferences original = CottonRemotePushPreferences.Default;
            CottonRemotePushPreferences changed = original
                .WithCategory(CottonRemotePushEventCategory.SharedFile, true)
                .WithCategory(CottonRemotePushEventCategory.SecuritySession, false);

            Assert.False(original.SharedFile);
            Assert.True(original.SecuritySession);
            Assert.True(changed.SharedFile);
            Assert.False(changed.SecuritySession);
            Assert.True(changed.IsEnabled(CottonRemotePushEventCategory.SharedFile));
            Assert.False(changed.IsEnabled(CottonRemotePushEventCategory.SecuritySession));
            Assert.Equal(1, changed.EnabledCategoryCount);
        }

        [Fact]
        public void Remote_push_preference_display_keeps_stable_order_and_copy()
        {
            CottonRemotePushPreferences preferences = CottonRemotePushPreferences.Default
                .WithCategory(CottonRemotePushEventCategory.SharedFile, true);

            CottonRemotePushPreferenceDisplayState display =
                CottonRemotePushPreferenceDisplayState.Create(preferences);

            Assert.Equal("2 server push categories on", display.SummaryText);
            Assert.Equal(
                new[]
                {
                    CottonRemotePushEventCategory.SharedFile,
                    CottonRemotePushEventCategory.AccessRequest,
                    CottonRemotePushEventCategory.CommentMention,
                    CottonRemotePushEventCategory.SecuritySession,
                },
                display.Items.Select(item => item.Category).ToArray());
            Assert.Equal("Shared-file activity", display.Items[0].Title);
            Assert.True(display.Items[0].IsEnabled);
            Assert.Equal("Security and sessions", display.Items[3].Title);
            Assert.True(display.Items[3].IsEnabled);
        }

        private static Dictionary<string, string> CreateRemotePushData(
            CottonRemotePushEventCategory category)
        {
            return new Dictionary<string, string>
            {
                [CottonRemotePushMessageDataKeys.NotificationId] = "11111111-2222-3333-4444-555555555555",
                [CottonRemotePushMessageDataKeys.EventCategory] = category.ToString(),
                [CottonRemotePushMessageDataKeys.Priority] = "Normal",
            };
        }
    }
}
