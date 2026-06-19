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
            if (BindingContext is not BackupSetupViewModel viewModel)
            {
                return;
            }

            if (_didLoad && !viewModel.ConsumeReloadOnAppearing())
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
