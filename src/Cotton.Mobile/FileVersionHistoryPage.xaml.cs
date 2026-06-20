using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class FileVersionHistoryPage : ContentPage
    {
        private bool _didLoad;

        public FileVersionHistoryPage(FileVersionHistoryViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not FileVersionHistoryViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
