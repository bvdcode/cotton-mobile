using Cotton.Mobile.Services;

namespace Cotton.Mobile.ViewModels
{
    public class RemotePushPreferenceItemViewModel : ViewModelBase
    {
        private bool _isEnabled;
        private bool _canToggle = true;
        private bool _isApplyingSourceValue;

        public RemotePushPreferenceItemViewModel(CottonRemotePushPreferenceDisplayItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            Category = item.Category;
            Title = item.Title;
            DetailText = item.DetailText;
            _isEnabled = item.IsEnabled;
        }

        public event EventHandler? ToggleRequested;

        public CottonRemotePushEventCategory Category { get; }

        public string Title { get; }

        public string DetailText { get; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetProperty(ref _isEnabled, value) && !_isApplyingSourceValue)
                {
                    ToggleRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool CanToggle
        {
            get => _canToggle;
            private set => SetProperty(ref _canToggle, value);
        }

        public void ApplySourceValue(bool isEnabled)
        {
            _isApplyingSourceValue = true;
            try
            {
                IsEnabled = isEnabled;
            }
            finally
            {
                _isApplyingSourceValue = false;
            }
        }

        public void SetCanToggle(bool canToggle)
        {
            CanToggle = canToggle;
        }
    }
}
