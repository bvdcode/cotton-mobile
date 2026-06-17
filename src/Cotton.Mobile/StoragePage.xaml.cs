using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class StoragePage : ContentPage
    {
        private bool _didLoad;

        public StoragePage(StorageSettingsViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not StorageSettingsViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
