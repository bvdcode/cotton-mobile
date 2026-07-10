using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class PermissionLedgerContractsTests
    {
        [Fact]
        public void Permission_ledger_reports_all_clear_when_core_access_is_allowed()
        {
            CottonPermissionLedgerDisplayState display = CottonPermissionLedgerDisplayState.Create(
                CottonNotificationPermissionState.Allowed,
                CottonCameraBackupMediaAccessState.Allowed);

            Assert.Equal("Device access", display.Title);
            Assert.Equal("Looks good", display.StatusText);
            Assert.Equal("Permissions Cotton uses on this device.", display.DetailText);
            Assert.True(display.HasItems);
            Assert.False(display.HasAttention);
            Assert.Equal(2, display.Items.Count);

            Assert.Collection(
                display.Items,
                item => AssertLedgerItem(
                    item,
                    "Notifications",
                    "Allowed",
                    needsAttention: false),
                item => AssertLedgerItem(
                    item,
                    "Photos and videos",
                    "Allowed",
                    needsAttention: false));
        }

        [Fact]
        public void Permission_ledger_surfaces_limited_or_missing_access()
        {
            CottonPermissionLedgerDisplayState display = CottonPermissionLedgerDisplayState.Create(
                CottonNotificationPermissionState.Denied,
                CottonCameraBackupMediaAccessState.Limited);

            Assert.Equal("2 items need review", display.StatusText);
            Assert.True(display.HasAttention);

            AssertLedgerItem(display.Items[0], "Notifications", "Denied", needsAttention: true);
            AssertLedgerItem(display.Items[1], "Photos and videos", "Selected media only", needsAttention: true);
        }

        [Fact]
        public void Permission_ledger_handles_unavailable_snapshot()
        {
            CottonPermissionLedgerDisplayState display =
                CottonPermissionLedgerDisplayState.Unavailable("Could not inspect device access.");

            Assert.Equal("Review needed", display.StatusText);
            Assert.True(display.HasAttention);
            CottonPermissionLedgerItem item = Assert.Single(display.Items);
            AssertLedgerItem(item, "Device access", "Unavailable", needsAttention: true);
            Assert.Equal("Could not inspect device access.", item.DetailText);
        }

        [Fact]
        public void Permission_ledger_items_reject_empty_text()
        {
            Assert.Throws<ArgumentException>(
                () => new CottonPermissionLedgerItem(" ", "Allowed", "Detail", needsAttention: false));
            Assert.Throws<ArgumentException>(
                () => new CottonPermissionLedgerItem("Title", " ", "Detail", needsAttention: false));
            Assert.Throws<ArgumentException>(
                () => new CottonPermissionLedgerItem("Title", "Allowed", " ", needsAttention: false));
        }

        private static void AssertLedgerItem(
            CottonPermissionLedgerItem item,
            string title,
            string statusText,
            bool needsAttention)
        {
            Assert.Equal(title, item.Title);
            Assert.Equal(statusText, item.StatusText);
            Assert.Equal(needsAttention, item.NeedsAttention);
            Assert.False(string.IsNullOrWhiteSpace(item.DetailText));
        }
    }
}
