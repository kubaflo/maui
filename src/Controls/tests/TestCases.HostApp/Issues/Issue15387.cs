namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 15387, "ScrollToAsync does not complete during initial OnAppearing", PlatformAffected.Android)]
public class Issue15387 : ContentPage
{
	readonly Label _appearanceCountLabel;
	readonly Label _scrollStartedLabel;
	readonly Label _completionStateLabel;
	readonly ScrollView _testScrollView;
	int _appearanceCount;

	public Issue15387()
	{
		_appearanceCountLabel = new Label
		{
			AutomationId = "Issue15387AppearanceCount",
			Text = "0"
		};
		_scrollStartedLabel = new Label
		{
			AutomationId = "Issue15387ScrollStarted",
			Text = "NotStarted"
		};
		_completionStateLabel = new Label
		{
			AutomationId = "Issue15387CompletionState",
			Text = "NotStarted"
		};

		var itemsLayout = new VerticalStackLayout();
		BindableLayout.SetItemTemplate(itemsLayout, new DataTemplate(() =>
		{
			var itemLabel = new Label
			{
				FontSize = 18,
				HeightRequest = 48
			};
			itemLabel.SetBinding(Label.TextProperty, ".");
			return itemLabel;
		}));
		BindableLayout.SetItemsSource(itemsLayout, new[]
		{
			"Constructor item 01",
			"Constructor item 02",
			"Constructor item 03",
			"Constructor item 04",
			"Constructor item 05",
			"Constructor item 06",
			"Constructor item 07",
			"Constructor item 08",
			"Constructor item 09",
			"Constructor item 10",
			"Constructor item 11",
			"Constructor item 12"
		});

		_testScrollView = new ScrollView
		{
			AutomationId = "Issue15387ScrollView",
			Content = itemsLayout
		};

		var grid = new Grid
		{
			Padding = 16,
			RowSpacing = 10,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		grid.Add(_appearanceCountLabel, 0, 0);
		grid.Add(_scrollStartedLabel, 0, 1);
		grid.Add(_completionStateLabel, 0, 2);
		grid.Add(_testScrollView, 0, 3);
		Content = grid;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		_appearanceCount++;
		_appearanceCountLabel.Text = _appearanceCount.ToString();
		if (_appearanceCount != 1)
			return;

		_scrollStartedLabel.Text = "Pending";
		_completionStateLabel.Text = "Pending";
		await _testScrollView.ScrollToAsync(0, 0, false);
		_completionStateLabel.Text = "Completed";
	}
}

