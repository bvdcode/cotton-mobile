using Cotton.Auth;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile
{
	public partial class MainPage : ContentPage
	{
		private const string InvalidUrlStatus = "Enter a valid HTTPS URL.";

		private readonly ICottonSessionService _sessionService;
		private readonly IBrowser _browser;
		private readonly ILogger<MainPage> _logger;

		private CancellationTokenSource? _authorizationCancellation;
		private bool _didRestoreSession;

		public MainPage(
			ICottonSessionService sessionService,
			IBrowser browser,
			ILogger<MainPage> logger)
		{
			ArgumentNullException.ThrowIfNull(sessionService);
			ArgumentNullException.ThrowIfNull(browser);
			ArgumentNullException.ThrowIfNull(logger);

			_sessionService = sessionService;
			_browser = browser;
			_logger = logger;
			InitializeComponent();
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			if (_didRestoreSession)
			{
				return;
			}

			_didRestoreSession = true;
			await RestoreSessionAsync();
		}

		private async void OnConnectClicked(object? sender, EventArgs e)
		{
			await SignInAsync();
		}

		private void OnCancelAuthorizationClicked(object? sender, EventArgs e)
		{
			CancelAuthorizationButton.IsEnabled = false;
			AuthorizationProgressLabel.Text = "Cancelling authorization...";
			_authorizationCancellation?.Cancel();
		}

		private async void OnLogoutClicked(object? sender, EventArgs e)
		{
			await LogoutAsync();
		}

		private async void OnPrivacyPolicyClicked(object? sender, EventArgs e)
		{
			await OpenPrivacyPolicyAsync();
		}

		private async Task RestoreSessionAsync()
		{
			ShowLoading("Restoring session...");
			try
			{
				CottonSessionResult result = await _sessionService.RestoreAsync();
				ApplySessionResult(result, "Ready to connect.");
			}
			catch (Exception exception)
			{
				_logger.LogWarning(exception, "Failed to restore Cotton mobile session.");
				ShowSignIn("Session restore failed. Sign in again.");
			}
		}

		private async Task SignInAsync()
		{
			Uri? instanceUri = ResolveInstanceUri();
			if (instanceUri is null)
			{
				ShowSignIn(InvalidUrlStatus);
				return;
			}

			InstanceUrlEntry.Text = instanceUri.AbsoluteUri;

			using var authorizationCancellation = new CancellationTokenSource();
			_authorizationCancellation = authorizationCancellation;
			ShowAuthorizationProgress(instanceUri);

			try
			{
				CottonSessionResult result = await _sessionService.SignInWithBrowserAsync(
					instanceUri,
					authorizationCancellation.Token);
				ApplySessionResult(result, "Ready to connect.");
			}
			catch (OperationCanceledException exception) when (authorizationCancellation.IsCancellationRequested)
			{
				_logger.LogInformation(exception, "Cotton mobile browser authorization was cancelled.");
				ShowSignIn("Authorization cancelled.");
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Cotton mobile browser authorization failed.");
				ShowSignIn(CreateAuthorizationFailureStatus(exception));
			}
			finally
			{
				if (ReferenceEquals(_authorizationCancellation, authorizationCancellation))
				{
					_authorizationCancellation = null;
				}
			}
		}

		private async Task LogoutAsync()
		{
			ShowLoading("Signing out...");
			try
			{
				await _sessionService.LogoutAsync();
				InstanceUrlEntry.Text = CottonApplicationLinks.DefaultInstanceUrl;
				ShowSignIn("Signed out.");
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Cotton mobile logout failed.");
				ShowProfileError("Logout failed. Try again.");
			}
		}

		private async Task OpenPrivacyPolicyAsync()
		{
			try
			{
				await _browser.OpenAsync(
					new Uri(CottonApplicationLinks.PrivacyPolicyUrl),
					CottonBrowserLaunchOptions.External());
			}
			catch (Exception exception)
			{
				_logger.LogWarning(exception, "Failed to open Cotton Cloud privacy policy.");
				await DisplayAlertAsync("Privacy Policy", "Could not open the privacy policy.", "OK");
			}
		}

		private Uri? ResolveInstanceUri()
		{
			Uri? instanceUri = CottonServerUrl.NormalizeOptional(InstanceUrlEntry.Text);
			if (instanceUri is null
				|| !string.Equals(instanceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
				|| string.IsNullOrWhiteSpace(instanceUri.Host))
			{
				return null;
			}

			return instanceUri;
		}

		private void ApplySessionResult(CottonSessionResult result, string unauthenticatedStatus)
		{
			if (result.InstanceUri is not null)
			{
				InstanceUrlEntry.Text = result.InstanceUri.AbsoluteUri;
			}

			if (result.IsAuthenticated && result.InstanceUri is not null && result.User is not null)
			{
				ShowProfile(result.InstanceUri, result.User);
				return;
			}

			ShowSignIn(ResolveStatusMessage(result, unauthenticatedStatus));
		}

		private void ShowLoading(string message)
		{
			SetVisible(loading: true, signIn: false, progress: false, profile: false);
			LoadingLabel.Text = message;
			LoadingIndicator.IsRunning = true;
			AuthorizationProgressIndicator.IsRunning = false;
			SetInputEnabled(isEnabled: false);
			SemanticScreenReader.Announce(message);
		}

		private void ShowSignIn(string? status)
		{
			SetVisible(loading: false, signIn: true, progress: false, profile: false);
			LoadingIndicator.IsRunning = false;
			AuthorizationProgressIndicator.IsRunning = false;
			SetInputEnabled(isEnabled: true);
			ShowStatus(status);
		}

		private void ShowAuthorizationProgress(Uri instanceUri)
		{
			SetVisible(loading: false, signIn: false, progress: true, profile: false);
			LoadingIndicator.IsRunning = false;
			AuthorizationProgressIndicator.IsRunning = true;
			CancelAuthorizationButton.IsEnabled = true;
			AuthorizationProgressLabel.Text = $"Approve the request for {instanceUri.Host}, then return to Cotton Cloud.";
			SetInputEnabled(isEnabled: false);
			SemanticScreenReader.Announce("Waiting for browser approval.");
		}

		private void ShowProfile(Uri instanceUri, UserDto user)
		{
			SetVisible(loading: false, signIn: false, progress: false, profile: true);
			LoadingIndicator.IsRunning = false;
			AuthorizationProgressIndicator.IsRunning = false;
			ProfileNameLabel.Text = CreateDisplayName(user);
			ProfileEmailLabel.Text = string.IsNullOrWhiteSpace(user.Email) ? "Email not set" : user.Email.Trim();
			ProfileInstanceLabel.Text = instanceUri.Host;
			LogoutButton.IsEnabled = true;
			SetInputEnabled(isEnabled: false);
			SemanticScreenReader.Announce("Signed in.");
		}

		private void ShowProfileError(string status)
		{
			SetVisible(loading: false, signIn: false, progress: false, profile: true);
			LoadingIndicator.IsRunning = false;
			AuthorizationProgressIndicator.IsRunning = false;
			LogoutButton.IsEnabled = true;
			SemanticScreenReader.Announce(status);
		}

		private void ShowStatus(string? status)
		{
			StatusLabel.Text = status;
			StatusLabel.IsVisible = !string.IsNullOrWhiteSpace(status);
			if (!string.IsNullOrWhiteSpace(status))
			{
				SemanticScreenReader.Announce(status);
			}
		}

		private void SetVisible(bool loading, bool signIn, bool progress, bool profile)
		{
			LoadingView.IsVisible = loading;
			SignInView.IsVisible = signIn;
			AuthorizationProgressView.IsVisible = progress;
			ProfileView.IsVisible = profile;
		}

		private void SetInputEnabled(bool isEnabled)
		{
			InstanceUrlEntry.IsEnabled = isEnabled;
			ConnectButton.IsEnabled = isEnabled;
		}

		private static string CreateDisplayName(UserDto user)
		{
			string fullName = string.Join(
				" ",
				new[] { user.FirstName, user.LastName }
					.Where(part => !string.IsNullOrWhiteSpace(part))
					.Select(part => part!.Trim()));
			return string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;
		}

		private static string ResolveStatusMessage(CottonSessionResult result, string unauthenticatedStatus)
		{
			return result.Status switch
			{
				CottonSessionResultStatus.AuthorizationDenied => "Authorization was denied.",
				CottonSessionResultStatus.AuthorizationExpired => "Authorization expired. Try again.",
				CottonSessionResultStatus.AuthorizationNotFound => "Authorization request was not found. Try again.",
				CottonSessionResultStatus.BrowserUnavailable => "Could not open the browser.",
				CottonSessionResultStatus.TimedOut => "Authorization timed out. Try again.",
				CottonSessionResultStatus.AuthorizationFailed => "Authorization failed. Try again.",
				_ => unauthenticatedStatus,
			};
		}

		private static string CreateAuthorizationFailureStatus(Exception exception)
		{
#if DEBUG
			return $"Authorization failed: {exception.GetType().Name}: {exception.Message}";
#else
			return "Authorization failed. Check the instance URL and try again.";
#endif
		}
	}
}
