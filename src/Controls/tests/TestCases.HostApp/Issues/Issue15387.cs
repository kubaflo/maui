namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 15387, "ScrollToAsync does not complete during initial page appearance", PlatformAffected.Android)]
public class Issue15387 : ContentPage
{
	readonly ScrollView testScrollView;
	readonly Label completionLogLabel;
	readonly Label lifecycleTokenLabel;
	readonly Label completionStateLabel;
	int completionState = -1;

	public Issue15387()
	{
		completionLogLabel = new Label
		{
			Text = "Waiting for OnAppearing",
			AutomationId = "Issue15387CompletionLog"
		};

		var itemsLayout = new VerticalStackLayout
		{
			Spacing = 8
		};

		BindableLayout.SetItemTemplate(itemsLayout, new DataTemplate(() =>
		{
			var itemLabel = new Label
			{
				Padding = 8
			};
			itemLabel.SetBinding(Label.TextProperty, ".");
			itemLabel.SetBinding(AutomationIdProperty, ".");
			return itemLabel;
		}));
		BindableLayout.SetItemsSource(itemsLayout, new[]
		{
			"Item 1",
			"Item 2",
			"Item 3",
			"Item 4",
			"Item 5",
			"Item 6",
			"Item 7",
			"Item 8",
			"Item 9",
			"Item 10",
			"Item 11",
			"Item 12"
		});

		testScrollView = new ScrollView
		{
			AutomationId = "Issue15387ScrollView",
			Content = itemsLayout
		};

		lifecycleTokenLabel = new Label
		{
			Text = "Lifecycle: -1",
			AutomationId = "Issue15387LifecycleToken"
		};

		completionStateLabel = new Label
		{
			Text = "Completion state: -1",
			AutomationId = "Issue15387CompletionState"
		};

		var grid = new Grid
		{
			Padding = 16,
			RowSpacing = 12,
			RowDefinitions =
			[
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			],
			Children =
			{
				completionLogLabel,
				testScrollView,
				lifecycleTokenLabel,
				completionStateLabel
			}
		};

		Grid.SetRow(testScrollView, 1);
		Grid.SetRow(lifecycleTokenLabel, 2);
		Grid.SetRow(completionStateLabel, 3);
		Content = grid;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (completionState != -1)
			return;

		completionState = 0;
		lifecycleTokenLabel.Text = "Lifecycle: -1->0";
		completionStateLabel.Text = "Completion state: 0";
		completionLogLabel.Text = "Before ScrollToAsync";

		await testScrollView.ScrollToAsync(0, 0, false);

		completionState = 1;
		completionStateLabel.Text = "Completion state: 1";
		completionLogLabel.Text += "\nAfter ScrollToAsync";
	}
}

