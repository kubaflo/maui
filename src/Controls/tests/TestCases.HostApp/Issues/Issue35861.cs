namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35861, "Android permission requests retain stale entries after off-main-thread failures", PlatformAffected.Android)]
public class Issue35861 : ContentPage
{
	readonly Button _triggerButton;
	readonly Label _detailsLabel;
	readonly Label _finalStateLabel;

	public Issue35861()
	{
		var titleLabel = new Label
		{
			AutomationId = "Issue35861Title",
			Text = "Android permission request-code collision",
			FontSize = 24
		};

		var explanationLabel = new Label
		{
			AutomationId = "Issue35861Explanation",
			Text = "Dismiss the first location prompt. The app will make 999 invalid off-main-thread requests, then one valid main-thread request."
		};

		_triggerButton = new Button
		{
			AutomationId = "Issue35861StartButton",
			Text = "Start permission requests"
		};
		_triggerButton.Clicked += OnStartPermissionRequestsClicked;

		_detailsLabel = new Label
		{
			AutomationId = "Issue35861Details",
			Text = "CALLBACK=NOT_STARTED; FAILURES=-1"
		};

		_finalStateLabel = new Label
		{
			AutomationId = "Issue35861FinalState",
			Text = "NOT_STARTED",
			FontAttributes = FontAttributes.Bold,
			FontSize = 20
		};

		Content = new ScrollView
		{
			AutomationId = "Issue35861ScrollView",
			Content = new VerticalStackLayout
			{
				AutomationId = "Issue35861Stack",
				Padding = 24,
				Spacing = 20,
				Children =
				{
					titleLabel,
					explanationLabel,
					_triggerButton,
					_detailsLabel,
					_finalStateLabel
				}
			}
		};
	}

	async void OnStartPermissionRequestsClicked(object sender, EventArgs e)
	{
		_triggerButton.IsEnabled = false;
		_detailsLabel.Text = "CALLBACK=WAITING_FOR_INITIAL_CALLBACK; FAILURES=-1";

		await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
		_detailsLabel.Text = "CALLBACK=INITIAL_CALLBACK_COMPLETED; FAILURES=-1";

		await System.Threading.Tasks.Task.CompletedTask.ConfigureAwait(
			System.Threading.Tasks.ConfigureAwaitOptions.ForceYielding);

		var expectedFailures = 0;
		for (var index = 0; index < 999; index++)
		{
			try
			{
				await Permissions.RequestAsync<Permissions.LocationWhenInUse>().ConfigureAwait(false);
			}
			catch (PermissionException)
			{
				expectedFailures++;
			}
		}

		var completedDetails = $"CALLBACK=INITIAL_CALLBACK_COMPLETED; FAILURES={expectedFailures.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
		await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			_detailsLabel.Text = completedDetails;
			_finalStateLabel.Text = "FINAL_REQUEST_STARTED";

			try
			{
				await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
				_finalStateLabel.Text = "COMPLETED";
				_detailsLabel.Text = completedDetails + "; FINAL=COMPLETED";
			}
			catch (ArgumentException exception) when (
				exception.Message.Contains("same key has already been added", StringComparison.Ordinal))
			{
				_finalStateLabel.Text = "DUPLICATE_REQUEST_CODE";
				_detailsLabel.Text = $"{completedDetails}; FINAL={exception.Message}";
			}
		});
	}
}

