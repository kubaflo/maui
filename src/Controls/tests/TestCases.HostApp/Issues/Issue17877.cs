namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 17877, "TabbedPage does not trigger when reselected current tab", PlatformAffected.Android)]
public class Issue17877 : TabbedPage
{
	int _currentPageChangedCount;
	int _postTriggerCount;
	bool _isArmed;
	readonly Label _currentPageChangedCountLabel;
	readonly Label _postTriggerCountLabel;

	public Issue17877()
	{
		_currentPageChangedCountLabel = new Label
		{
			AutomationId = "CurrentPageChangedCount",
			Text = "CurrentPageChanged count: 0"
		};

		_postTriggerCountLabel = new Label
		{
			AutomationId = "PostTriggerCount",
			Text = "Post-trigger count: not armed"
		};

		var armButton = new Button
		{
			AutomationId = "ArmReselectionCheck",
			Text = "Arm reselection check"
		};
		armButton.Clicked += OnArmClicked;

		var checkButton = new Button
		{
			AutomationId = "CheckReselectionEvent",
			Text = "Check reselection event"
		};
		checkButton.Clicked += OnCheckClicked;

		var tabOne = new ContentPage
		{
			Title = "Tab 1",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						AutomationId = "Tab1Content",
						FontSize = 24,
						Text = "Tab 1 content"
					},
					new Label { Text = "Reselect Tab 1, then check whether the event fired." },
					_currentPageChangedCountLabel,
					_postTriggerCountLabel,
					armButton,
					checkButton
				}
			}
		};

		var tabTwo = new ContentPage
		{
			Title = "Tab 2",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Children =
				{
					new Label
					{
						AutomationId = "Tab2Content",
						FontSize = 24,
						Text = "Tab 2 content"
					}
				}
			}
		};

		Children.Add(tabOne);
		Children.Add(tabTwo);
		CurrentPage = tabOne;
		CurrentPageChanged += OnCurrentPageChanged;

#if ANDROID
		Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage.SetToolbarPlacement(
			this,
			Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Bottom);
#endif
	}

	void OnCurrentPageChanged(object sender, EventArgs e)
	{
		_currentPageChangedCount++;
		_currentPageChangedCountLabel.Text = $"CurrentPageChanged count: {_currentPageChangedCount}";

		if (_isArmed)
		{
			_postTriggerCount = _currentPageChangedCount;
			_postTriggerCountLabel.Text = $"Post-trigger count: {_postTriggerCount}";
		}
	}

	void OnArmClicked(object sender, EventArgs e)
	{
		_postTriggerCount = -1;
		_isArmed = true;
		_postTriggerCountLabel.Text = "Post-trigger count: -1";
	}

	void OnCheckClicked(object sender, EventArgs e)
	{
		_postTriggerCountLabel.Text = $"Post-trigger count: {_postTriggerCount}";
	}
}

