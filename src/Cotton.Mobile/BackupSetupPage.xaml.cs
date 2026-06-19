using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class BackupSetupPage : ContentPage
    {
        private bool _didLoad;

        public BackupSetupPage(BackupSetupViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not BackupSetupViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
