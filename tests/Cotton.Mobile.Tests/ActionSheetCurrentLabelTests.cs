using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ActionSheetCurrentLabelTests
    {
        [Fact]
        public void Create_marks_current_action_with_accessible_suffix()
        {
            Assert.Equal("Newest (current)", CottonActionSheetCurrentLabel.Create("Newest", isCurrent: true));
            Assert.Equal("Newest", CottonActionSheetCurrentLabel.Create("Newest", isCurrent: false));
        }

        [Fact]
        public void Normalize_removes_current_suffix_from_selected_action()
        {
            Assert.Equal("Tiles", CottonActionSheetCurrentLabel.Normalize("Tiles (current)"));
            Assert.Equal("Tiles", CottonActionSheetCurrentLabel.Normalize("Tiles"));
            Assert.Null(CottonActionSheetCurrentLabel.Normalize(null));
        }

        [Fact]
        public void Display_label_strips_current_suffix_and_reports_selection()
        {
            bool selected = CottonActionSheetCurrentLabel.TryCreateDisplayLabel(
                "List (current)",
                out string displayLabel);

            Assert.True(selected);
            Assert.Equal("List", displayLabel);
        }
    }
}
