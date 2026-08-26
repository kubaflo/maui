namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26182, "CollectionView items are not selected when a parent has a TapGestureRecognizer", PlatformAffected.iOS)]
public class Issue26182 : ContentPage
{
	int _buttonClickCount;
	int _parentTapCount;
	bool _resetting;

	public Issue26182()
	{
		var buttonStatus = new Label
		{
			Text = "Button clicked: 0",
			AutomationId = "ButtonStatus"
		};

		var parentStatus = new Label
		{
			Text = "Parent taps: 0",
			AutomationId = "ParentStatus"
		};

		var selectionStatus = new Label
		{
			Text = "Selected item: none",
			AutomationId = "SelectionStatus"
		};

		var resetStatus = new Label
		{
			Text = "Reset complete: 0",
			AutomationId = "ResetStatus"
		};

		var collectionView = new CollectionView
		{
			AutomationId = "ItemsCollection",
			ItemsSource = new[] { "Hello", "World" },
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, ".");
				return label;
			})
		};

		collectionView.SelectionChanged += (_, e) =>
		{
			if (!_resetting && e.CurrentSelection.Count > 0 && e.CurrentSelection[0] is string selectedItem)
				selectionStatus.Text = $"Selected item: {selectedItem}";
		};

		var referenceButton = new Button
		{
			Text = "Hello World Button",
			AutomationId = "HelloWorldButton"
		};

		referenceButton.Clicked += (_, _) =>
		{
			_buttonClickCount++;
			buttonStatus.Text = $"Button clicked: {_buttonClickCount}";
		};

		var gestureContainer = new VerticalStackLayout
		{
			Spacing = 12,
			Children =
			{
				collectionView,
				referenceButton
			}
		};

		var parentTapGesture = new TapGestureRecognizer();
		parentTapGesture.Tapped += (_, _) =>
		{
			_parentTapCount++;
			parentStatus.Text = $"Parent taps: {_parentTapCount}";
		};
		gestureContainer.GestureRecognizers.Add(parentTapGesture);

		var resetCount = 0;
		var resetButton = new Button
		{
			Text = "Reset for next attempt",
			AutomationId = "ResetButton"
		};

		resetButton.Clicked += (_, _) =>
		{
			_resetting = true;
			collectionView.SelectedItem = null;
			_resetting = false;
			selectionStatus.Text = "Selected item: none";
			resetCount++;
			resetStatus.Text = $"Reset complete: {resetCount}";
		};

		var statusLayout = new VerticalStackLayout
		{
			Spacing = 4,
			Children =
			{
				buttonStatus,
				parentStatus,
				selectionStatus,
				resetStatus,
				resetButton
			}
		};

		var grid = new Grid
		{
			Padding = 20,
			RowSpacing = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};

		grid.Add(new Label
		{
			Text = "CollectionView parent gesture test",
			AutomationId = "ScenarioTitle"
		});
		grid.Add(gestureContainer, 0, 1);
		grid.Add(statusLayout, 0, 2);

		Content = grid;
	}
}

