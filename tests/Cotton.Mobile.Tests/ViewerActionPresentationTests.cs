using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ViewerActionPresentationTests
    {
        [Fact]
        public void Viewer_system_actions_do_not_show_transient_busy_status()
        {
            string[] viewModelPaths =
            [
                "src/Cotton.Mobile/ViewModels/ImageViewerViewModel.cs",
                "src/Cotton.Mobile/ViewModels/MediaViewerViewModel.cs",
                "src/Cotton.Mobile/ViewModels/PdfViewerViewModel.cs",
                "src/Cotton.Mobile/ViewModels/TextViewerViewModel.cs",
            ];

            foreach (string viewModelPath in viewModelPaths)
            {
                string content = RepositoryPath.ReadText(viewModelPath);

                Assert.DoesNotContain("\"Opening...\"", content, StringComparison.Ordinal);
                Assert.DoesNotContain("\"Preparing share...\"", content, StringComparison.Ordinal);
                Assert.DoesNotContain("\"Copying...\"", content, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Android_pdf_preview_renders_bounded_page_sample()
        {
            string renderer = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Platforms/Android/AndroidPdfPreviewRenderer.cs");
            string snapshot = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Services/PdfPreviewDocumentSnapshot.cs");

            Assert.Contains("MaxPreviewPageCount = 8", renderer, StringComparison.Ordinal);
            Assert.Contains("int renderedPageCount = Math.Min(renderer.PageCount, MaxPreviewPageCount);", renderer, StringComparison.Ordinal);
            Assert.Contains("new List<PdfPreviewPageSnapshot>(renderedPageCount)", renderer, StringComparison.Ordinal);
            Assert.Contains("index < renderedPageCount", renderer, StringComparison.Ordinal);
            Assert.Contains("Pages.Count > 0 && Pages.Count < TotalPageCount", snapshot, StringComparison.Ordinal);
            Assert.Contains("Showing first {Pages.Count} of {TotalPageCount} pages", snapshot, StringComparison.Ordinal);
        }

        [Fact]
        public void Audio_viewer_has_complete_transport_and_timeline_states()
        {
            string app = RepositoryPath.ReadText("src/Cotton.Mobile/App.xaml");
            string page = RepositoryPath.ReadText("src/Cotton.Mobile/MediaViewerPage.xaml");
            string codeBehind = RepositoryPath.ReadText("src/Cotton.Mobile/MediaViewerPage.xaml.cs");
            string player = RepositoryPath.ReadText("src/Cotton.Mobile/Controls/AudioPlayerView.cs");
            string mediaElement = RepositoryPath.ReadText("src/Cotton.Mobile/Controls/ViewerMediaElementView.cs");
            string audioStyles = RepositoryPath.ReadText("src/Cotton.Mobile/Resources/Styles/AudioPlayerStyles.xaml");
            string sharedStyles = RepositoryPath.ReadText("src/Cotton.Mobile/Resources/Styles/Styles.xaml");

            Assert.Contains("Source=\"Resources/Styles/AudioPlayerStyles.xaml\"", app, StringComparison.Ordinal);
            Assert.Contains("<controls:AudioPlayerView x:Name=\"AudioPlayer\"", page, StringComparison.Ordinal);
            Assert.Contains("SemanticProperties.SetDescription(_timeline, \"Audio position\")", player, StringComparison.Ordinal);
            Assert.Contains("_playbackButton.IconData = IsPlaying ? IconPathData.Pause : IconPathData.Play", player, StringComparison.Ordinal);
            Assert.Contains("IsPlaying ? \"Pause audio\" : \"Play audio\"", player, StringComparison.Ordinal);
            Assert.Contains("SeekRequested?.Invoke(this, new AudioSeekRequestedEventArgs(position))", player, StringComparison.Ordinal);
            Assert.Contains("CottonMediaTimeFormatter.Format(previewPosition)", player, StringComparison.Ordinal);
            Assert.Contains("UpdateAudioPosition(e.Position)", codeBehind, StringComparison.Ordinal);
            Assert.Contains("UpdateAudioPosition(_mediaDuration)", codeBehind, StringComparison.Ordinal);
            Assert.Contains("AndroidViewType = AndroidViewType.TextureView", mediaElement, StringComparison.Ordinal);
            Assert.Contains("HiddenAudioSurfaceSize = 1", codeBehind, StringComparison.Ordinal);
            Assert.Contains("MediaPlayer.WidthRequest = HiddenAudioSurfaceSize", codeBehind, StringComparison.Ordinal);
            Assert.Contains("MediaPlayer.HeightRequest = HiddenAudioSurfaceSize", codeBehind, StringComparison.Ordinal);
            Assert.Contains("RefreshAudioDuration()", codeBehind, StringComparison.Ordinal);
            Assert.Contains("AudioPlayer.Duration = duration", codeBehind, StringComparison.Ordinal);
            Assert.True(
                codeBehind.IndexOf("PlayMediaCommand = new Command(PlayMedia)", StringComparison.Ordinal)
                < codeBehind.IndexOf("InitializeComponent()", StringComparison.Ordinal));
            Assert.Contains("x:Key=\"M3AudioPlayerLayer\"", audioStyles, StringComparison.Ordinal);
            Assert.Contains("x:Key=\"M3AudioPlaybackIconButton\"", audioStyles, StringComparison.Ordinal);
            Assert.DoesNotContain("M3AudioPlayerLayer", sharedStyles, StringComparison.Ordinal);
        }
    }
}
