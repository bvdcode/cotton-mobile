using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class SecuritySettingsPage : ContentPage
    {
        private bool _didLoad;

        public SecuritySettingsPage(SecuritySettingsViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not SecuritySettingsViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
