#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 15387, "ScrollToAsync does not return during initial OnAppearing", PlatformAffected.Android)]
public class Issue15387 : ContentPage
{
	readonly ScrollView _testScrollView;
	readonly Label _startedLabel;
	readonly Label _completedLabel;

	public Issue15387()
	{
		_startedLabel = new Label
		{
			Text = "Started: -1",
			AutomationId = "Issue15387Started"
		};

		_completedLabel = new Label
		{
			Text = "Completed: -1",
			AutomationId = "Issue15387Completed"
		};

		var itemsLayout = new VerticalStackLayout();
		BindableLayout.SetItemTemplate(itemsLayout, new DataTemplate(() =>
		{
			var itemLabel = new Label();
			itemLabel.SetBinding(Label.TextProperty, ".");

			return new Grid
			{
				Padding = 8,
				Children = { itemLabel }
			};
		}));

		var items = new List<string>();
		for (var index = 1; index <= 20; index++)
			items.Add($"Bindable item {index}");

		BindableLayout.SetItemsSource(itemsLayout, items);

		_testScrollView = new ScrollView
		{
			AutomationId = "Issue15387ScrollView",
			Content = itemsLayout
		};

		var grid = new Grid
		{
			Padding = 16,
			RowSpacing = 8,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};

		grid.Add(new Label { Text = "ScrollToAsync from initial OnAppearing" }, 0, 0);
		grid.Add(_startedLabel, 0, 1);
		grid.Add(_completedLabel, 0, 2);
		grid.Add(_testScrollView, 0, 3);
		Content = grid;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		_startedLabel.Text = "Started: 1";
		await _testScrollView.ScrollToAsync(0, 0, false);
		_completedLabel.Text = "Completed: 1";
	}
}
#endif

