using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class CaptureDestinationPickerPage : ContentPage
    {
        private bool _didLoad;

        public CaptureDestinationPickerPage(CaptureDestinationPickerViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not CaptureDestinationPickerViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
