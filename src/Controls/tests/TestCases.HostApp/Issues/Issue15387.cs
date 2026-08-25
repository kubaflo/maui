namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 15387, "ScrollToAsync does not return from initial OnAppearing", PlatformAffected.Android)]
public class Issue15387 : ContentPage
{
	readonly Label _appearingStateLabel;
	readonly Label _completionStateLabel;
	readonly ScrollView _testScrollView;
	bool _isFirstAppearance = true;

	public Issue15387()
	{
		_appearingStateLabel = new Label
		{
			AutomationId = "AppearingState",
			Text = "NOT_STARTED"
		};

		_completionStateLabel = new Label
		{
			AutomationId = "CompletionState",
			Text = "NOT_STARTED"
		};

		var itemsLayout = new VerticalStackLayout();
		BindableLayout.SetItemTemplate(itemsLayout, new DataTemplate(() =>
		{
			var itemLabel = new Label();
			itemLabel.SetBinding(Label.TextProperty, ".");
			return itemLabel;
		}));
		BindableLayout.SetItemsSource(itemsLayout, new[]
		{
			"Item 1",
			"Item 2",
			"Item 3"
		});

		_testScrollView = new ScrollView
		{
			Content = itemsLayout
		};

		var grid = new Grid
		{
			Padding = 16,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 12
		};

		grid.Add(new Label
		{
			Text = "Initial OnAppearing ScrollToAsync",
			FontAttributes = FontAttributes.Bold
		}, row: 0);
		grid.Add(_appearingStateLabel, row: 1);
		grid.Add(new VerticalStackLayout
		{
			Spacing = 8,
			Children = { _completionStateLabel }
		}, row: 2);
		grid.Add(_testScrollView, row: 3);

		Content = grid;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (!_isFirstAppearance)
			return;

		_isFirstAppearance = false;
		_appearingStateLabel.Text = "STARTED";
		_completionStateLabel.Text = "STARTED";

		await _testScrollView.ScrollToAsync(0, 0, false);

		_completionStateLabel.Text = "COMPLETED";
	}
}

