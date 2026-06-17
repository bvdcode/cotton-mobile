using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class TextViewerPage : ContentPage
    {
        public TextViewerPage(TextViewerViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
