using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class AppLockGatePage : ContentPage
    {
        public AppLockGatePage(AppLockGateViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}
