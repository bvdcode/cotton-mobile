using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class TrashPage : ContentPage
    {
        private bool _didLoad;

        public TrashPage(TrashViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not TrashViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
