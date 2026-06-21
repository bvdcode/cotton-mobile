using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class RecentFilesPage : ContentPage
    {
        private bool _didLoad;

        public RecentFilesPage(RecentFilesViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not RecentFilesViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
