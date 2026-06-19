using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class MediaViewerPage : ContentPage
    {
        public MediaViewerPage(MediaViewerViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnDisappearing()
        {
            MediaPlayer.Stop();

            base.OnDisappearing();
        }
    }
}
