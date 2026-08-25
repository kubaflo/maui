using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32587, "ContentView inside CollectionView reports invalid bounds during gesture events", PlatformAffected.UWP)]
public class Issue32587 : ContentPage
{
	public Issue32587()
	{
		var referenceCollection = new CollectionView
		{
			AutomationId = "ReferenceCollection",
			SelectionMode = SelectionMode.None,
			ItemsSource = new[] { "Item" },
			ItemTemplate = new DataTemplate(() =>
			{
				var item = new Issue32587BoundsContentView("Reference", "ReferenceItem", "ReferenceObservation");
				return new Grid
				{
					Children = { item }
				};
			})
		};

		var directCollection = new CollectionView
		{
			AutomationId = "DirectCollection",
			SelectionMode = SelectionMode.None,
			ItemsSource = new[] { "Item" },
			ItemTemplate = new DataTemplate(() =>
				new Issue32587BoundsContentView("Direct", "DirectItem", "DirectObservation"))
		};

		var layout = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 16,
			Children =
			{
				new Label
				{
					AutomationId = "SceneState",
					Text = "One item; default styling; unconstrained sizing."
				}
			}
		};

		Grid.SetRow(referenceCollection, 1);
		Grid.SetRow(directCollection, 2);
		layout.Add(referenceCollection);
		layout.Add(directCollection);
		Content = layout;
	}
}

sealed class Issue32587BoundsContentView : ContentView
{
	readonly string _itemKind;
	readonly Label _observationLabel;
	int _observationSequence = -1;

	public Issue32587BoundsContentView(string itemKind, string automationId, string observationAutomationId)
	{
		_itemKind = itemKind;
		AutomationId = automationId;

		_observationLabel = new Label
		{
			AutomationId = observationAutomationId
		};
		_observationLabel.SetBinding(
			Label.TextProperty,
			new Binding(".", stringFormat: $"{itemKind} item: {{0}}; sequence={_observationSequence}; width=unobserved; height=unobserved"));
		Content = _observationLabel;

		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += (_, _) => ObserveBounds();
		GestureRecognizers.Add(tapGesture);
	}

	void ObserveBounds()
	{
		_observationSequence = 1;
		_observationLabel.Text = string.Create(
			CultureInfo.InvariantCulture,
			$"{_itemKind} item observed; sequence={_observationSequence}; width={Width}; height={Height}");
	}
}

