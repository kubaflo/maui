#if WINDOWS
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35617, "Horizontal CollectionView delays rendering newly added items", PlatformAffected.UWP)]
public class Issue35617 : ContentPage
{
	const int RequiredCycles = 3;
	readonly ObservableCollection<RenderedItem> _items = new();
	readonly Label _callbackCountLabel;
	readonly Label _cycleCountLabel;
	readonly Label _itemCountLabel;
	int _callbackCount;
	int _completedCycles;

	public Issue35617()
	{
		_callbackCountLabel = new Label
		{
			Text = "-1",
			AutomationId = "Issue35617CallbackCount"
		};
		_cycleCountLabel = new Label
		{
			Text = "0",
			AutomationId = "Issue35617CycleCount"
		};
		_itemCountLabel = new Label
		{
			Text = "1",
			AutomationId = "Issue35617ItemCount"
		};

		var instructions = new StackLayout
		{
			Children =
			{
				new Label { Text = "Every rapidly added item should render immediately." },
				_callbackCountLabel,
				_cycleCountLabel,
				_itemCountLabel
			}
		};

		var addButton = new Button
		{
			Text = "Add",
			AutomationId = "Issue35617AddButton",
			HorizontalOptions = LayoutOptions.Start,
			VerticalOptions = LayoutOptions.Start
		};
		addButton.Clicked += OnAddClicked;

		var resetButton = new Button
		{
			Text = "Reset cycle",
			AutomationId = "Issue35617ResetButton",
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Start
		};
		resetButton.Clicked += OnResetClicked;

		var collectionView = new CollectionView
		{
			AutomationId = "Issue35617CollectionView",
			BackgroundColor = Colors.LightSalmon,
			HeightRequest = 50,
			ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal),
			ItemsSource = _items
		};

		var collectionGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				collectionView,
				new Label
				{
					Text = "A Label under the CollectionView",
					AutomationId = "Issue35617UnderlyingLabel"
				}.Row(1)
			}
		};

		Content = new Grid
		{
			Margin = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				instructions,
				addButton.Row(1),
				resetButton.Row(1),
				collectionGrid.Row(2)
			}
		};

		_items.Add(new RenderedItem("item: 0"));
	}

	void OnAddClicked(object sender, EventArgs e)
	{
		_items.Add(new RenderedItem($"item: {_items.Count}"));
		_itemCountLabel.Text = _items.Count.ToString();

		_callbackCount++;
		_callbackCountLabel.Text = _callbackCount.ToString();
	}

	void OnResetClicked(object sender, EventArgs e)
	{
		_completedCycles++;
		_cycleCountLabel.Text = _completedCycles.ToString();

		if (_completedCycles == RequiredCycles)
			return;

		_items.Clear();
		_items.Add(new RenderedItem("item: 0"));
		_itemCountLabel.Text = _items.Count.ToString();
	}

	sealed class RenderedItem
	{
		readonly string _text;

		public RenderedItem(string text)
		{
			_text = text;
		}

		public override string ToString() => _text;
	}
}
#endif

