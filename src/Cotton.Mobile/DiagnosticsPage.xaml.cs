using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class DiagnosticsPage : ContentPage
    {
        private bool _didLoad;

        public DiagnosticsPage(DiagnosticsViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not DiagnosticsViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
