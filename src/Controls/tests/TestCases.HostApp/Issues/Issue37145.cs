namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37145, "RadioButton border does not clear when BorderColor is reset", PlatformAffected.Android)]
public partial class Issue37145 : ContentPage
{
	readonly RadioButton _issueRadioButton;
	readonly Label _apiStateLabel;
	readonly Label _resultLabel;

	bool _resetBorderOnNextTap = true;
	int _renderGeneration = -1;

	public Issue37145()
	{
		var headingLabel = new Label
		{
			FontSize = 20,
			Text = "RadioButton border reset"
		};

		_issueRadioButton = new RadioButton
		{
			AutomationId = "Issue37145RadioButton",
			BorderColor = Colors.Red,
			BorderWidth = 4,
			CornerRadius = 12,
			Content = "RadioButton"
		};
		_issueRadioButton.Loaded += (_, _) =>
		{
			_renderGeneration = 0;
			ScheduleRenderMeasurement(_renderGeneration);
		};

		var toggleButton = new Button
		{
			AutomationId = "Issue37145Button",
			Text = "Button"
		};
		toggleButton.Clicked += OnToggleBorderColorClicked;

		_apiStateLabel = new Label
		{
			AutomationId = "Issue37145ApiState",
			Text = "BorderColor API state: Red"
		};

		_resultLabel = new Label
		{
			AutomationId = "Issue37145Result",
			Text = "generation=-1"
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 24,
			Children =
			{
				headingLabel,
				_issueRadioButton,
				toggleButton,
				_apiStateLabel,
				_resultLabel
			}
		};
	}

	void OnToggleBorderColorClicked(object sender, EventArgs e)
	{
		if (_resetBorderOnNextTap)
		{
			_issueRadioButton.BorderColor = null;
			_apiStateLabel.Text = "BorderColor API state: null";
		}
		else
		{
			_issueRadioButton.BorderColor = Colors.Blue;
			_apiStateLabel.Text = "BorderColor API state: Blue";
		}

		_resetBorderOnNextTap = !_resetBorderOnNextTap;
		ScheduleRenderMeasurement(++_renderGeneration);
	}

	partial void ScheduleRenderMeasurement(int generation);
}
