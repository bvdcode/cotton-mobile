using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class TransfersPage : ContentPage
    {
        private bool _didLoad;

        public TransfersPage(TransfersViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not TransfersViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
