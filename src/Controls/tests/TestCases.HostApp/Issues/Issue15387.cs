namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 15387, "ScrollToAsync called from initial OnAppearing does not complete", PlatformAffected.Android)]
public class Issue15387 : ContentPage
{
	const string CompletionPending = "Waiting for ScrollToAsync completion";
	const string CompletionSucceeded = "ScrollToAsync completed after initial OnAppearing";

	readonly Label _lifecycleStateLabel;
	readonly Label _completionStateLabel;
	readonly ScrollView _testScrollView;
	int _appearingCount;

	public Issue15387()
	{
		_lifecycleStateLabel = new Label
		{
			AutomationId = "Issue15387LifecycleState",
			Text = "OnAppearing not reached"
		};

		_completionStateLabel = new Label
		{
			AutomationId = "Issue15387CompletionState",
			Text = CompletionPending
		};

		var itemCountLabel = new Label
		{
			AutomationId = "Issue15387ItemCount",
			Text = "Item count: not initialized"
		};

		var itemsLayout = new StackLayout();
		BindableLayout.SetItemTemplate(itemsLayout, new DataTemplate(() =>
		{
			var itemLabel = new Label();
			itemLabel.SetBinding(Label.TextProperty, ".");

			var descriptionLabel = new Label
			{
				Text = "BindableLayout item initialized in the page constructor"
			};

			var itemGrid = new Grid
			{
				Padding = 8,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				},
				Children =
				{
					itemLabel,
					descriptionLabel
				}
			};
			Grid.SetRow(descriptionLabel, 1);

			return itemGrid;
		}));

		BindableLayout.SetItemsSource(itemsLayout, Enumerable.Range(1, 60)
			.Select(index => $"Item {index:00}")
			.ToArray());
		itemCountLabel.Text = $"Item count: {itemsLayout.Children.Count}";

		_testScrollView = new ScrollView
		{
			AutomationId = "Issue15387ScrollView",
			Content = itemsLayout
		};

		var rootGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				_lifecycleStateLabel,
				_completionStateLabel,
				itemCountLabel,
				_testScrollView
			}
		};

		Grid.SetRow(_completionStateLabel, 1);
		Grid.SetRow(itemCountLabel, 2);
		Grid.SetRow(_testScrollView, 3);
		Content = rootGrid;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		_appearingCount++;
		_lifecycleStateLabel.Text = $"OnAppearing callback {_appearingCount} reached ScrollToAsync";

		if (_appearingCount != 1)
			return;

		await _testScrollView.ScrollToAsync(0, 0, false);

		_completionStateLabel.Text = CompletionSucceeded;
	}
}

