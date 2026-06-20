using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class SyncSettingsPage : ContentPage
    {
        private bool _didLoad;

        public SyncSettingsPage(SyncSettingsViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not SyncSettingsViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
