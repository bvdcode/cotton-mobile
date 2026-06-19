using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class CaptureInboxPage : ContentPage
    {
        private bool _didLoad;

        public CaptureInboxPage(CaptureInboxViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not CaptureInboxViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
