using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TrashEmptyStatusTextTests
    {
        [Fact]
        public void Trash_empty_action_is_enabled_only_for_loaded_idle_trash()
        {
            CottonTrashEmptyActionSnapshot action = CottonTrashEmptyActionSnapshot.Create(
                itemCount: 3,
                isBusy: false,
                isSelectionModeActive: false);

            Assert.True(action.IsEnabled);
            Assert.Equal(3, action.ItemCount);
            Assert.Equal("Empty trash", action.Label);
            Assert.Equal(string.Empty, action.DisabledReason);
            Assert.Equal(
                "Permanently delete all 3 trash items? This cannot be undone.",
                action.ConfirmationMessage);
        }

        [Fact]
        public void Trash_empty_action_explains_disabled_states()
        {
            CottonTrashEmptyActionSnapshot empty = CottonTrashEmptyActionSnapshot.Create(
                itemCount: 0,
                isBusy: false,
                isSelectionModeActive: false);
            CottonTrashEmptyActionSnapshot busy = CottonTrashEmptyActionSnapshot.Create(
                itemCount: 2,
                isBusy: true,
                isSelectionModeActive: false);
            CottonTrashEmptyActionSnapshot selecting = CottonTrashEmptyActionSnapshot.Create(
                itemCount: 2,
                isBusy: false,
                isSelectionModeActive: true);

            Assert.False(empty.IsEnabled);
            Assert.Equal("Trash is empty.", empty.DisabledReason);
            Assert.Equal(string.Empty, empty.ConfirmationMessage);
            Assert.False(busy.IsEnabled);
            Assert.Equal("Trash is busy.", busy.DisabledReason);
            Assert.False(selecting.IsEnabled);
            Assert.Equal("Finish selecting trash items first.", selecting.DisabledReason);
        }

        [Fact]
        public void Trash_empty_status_text_formats_confirmation_and_progress()
        {
            Assert.Equal("Empty trash?", CottonTrashEmptyStatusText.ConfirmTitle);
            Assert.Equal("Empty trash", CottonTrashEmptyStatusText.ConfirmAction);
            Assert.Equal(
                "Permanently delete 1 trash item? This cannot be undone.",
                CottonTrashEmptyStatusText.CreateConfirmMessage(1));
            Assert.Equal(
                "Permanently delete all 3 trash items? This cannot be undone.",
                CottonTrashEmptyStatusText.CreateConfirmMessage(3));
            Assert.Equal("Deleting trash item...", CottonTrashEmptyStatusText.CreateDeletingStatus(1));
            Assert.Equal("Deleting 3 trash items...", CottonTrashEmptyStatusText.CreateDeletingStatus(3));
            Assert.Equal(
                "Deleting 2 of 3: Archive",
                CottonTrashEmptyStatusText.CreateDeletingItemStatus(2, 3, " Archive "));
            Assert.Equal(
                "Deleting 2 of 3: item",
                CottonTrashEmptyStatusText.CreateDeletingItemStatus(2, 3, " "));
        }

        [Fact]
        public void Trash_empty_status_text_formats_results_and_failures()
        {
            Assert.Equal("1 trash item deleted forever.", CottonTrashEmptyStatusText.CreateDeletedStatus(1));
            Assert.Equal("3 trash items deleted forever.", CottonTrashEmptyStatusText.CreateDeletedStatus(3));
            Assert.Equal(
                "1 of 3 trash items deleted forever. Review remaining items.",
                CottonTrashEmptyStatusText.CreatePartialDeleteStatus(1, 3));
            Assert.Equal("Empty trash cancelled.", CottonTrashEmptyStatusText.CancelledStatus);
            Assert.Equal("Could not empty trash.", CottonTrashEmptyStatusText.FailedStatus);
            Assert.Equal(
                "Offline. Empty trash needs internet.",
                CottonTrashEmptyStatusText.OfflineUnavailableStatus);
        }

        [Fact]
        public void Trash_empty_status_text_rejects_invalid_counts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonTrashEmptyActionSnapshot.Create(-1, isBusy: false, isSelectionModeActive: false));
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashEmptyStatusText.CreateConfirmMessage(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashEmptyStatusText.CreateDeletingStatus(-1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonTrashEmptyStatusText.CreateDeletingItemStatus(0, 1, "item"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonTrashEmptyStatusText.CreateDeletingItemStatus(2, 1, "item"));
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashEmptyStatusText.CreateDeletedStatus(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashEmptyStatusText.CreatePartialDeleteStatus(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashEmptyStatusText.CreatePartialDeleteStatus(2, 2));
        }
    }
}
