#if ANDROID
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28542, "CollectionView scrollbar thumb changes size for variable-height items", PlatformAffected.Android)]
public class Issue28542 : ContentPage
{
	public Issue28542()
	{
		var items = new ObservableCollection<VariableHeightItem>();
		for (var i = 1; i <= 12; i++)
			items.Add(new($"Short item {i:00}", $"ShortItem{i:00}", 56, Colors.LightBlue));
		for (var i = 1; i <= 16; i++)
			items.Add(new($"Tall item {i:00}", $"TallItem{i:00}", 190, Colors.MistyRose));

		var collection = new CollectionView
		{
			AutomationId = "Issue28542Collection",
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					VerticalOptions = LayoutOptions.Center,
					FontSize = 16
				};
				label.SetBinding(Label.TextProperty, nameof(VariableHeightItem.Text));
				label.SetBinding(AutomationIdProperty, nameof(VariableHeightItem.AutomationId));

				var itemGrid = new Grid { Padding = new Thickness(12, 4) };
				itemGrid.SetBinding(HeightRequestProperty, nameof(VariableHeightItem.Height));
				itemGrid.SetBinding(BackgroundColorProperty, nameof(VariableHeightItem.Color));
				itemGrid.Add(label);
				return itemGrid;
			})
		};
		collection.SetBinding(ItemsView.ItemsSourceProperty, ".");
		var scrollState = new Label
		{
			Text = "pending",
			AutomationId = "Issue28542ScrollState"
		};
		collection.Scrolled += (_, e) =>
			scrollState.Text = $"first={e.FirstVisibleItemIndex};last={e.LastVisibleItemIndex}";

		var pageGrid = new Grid
		{
			Padding = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 8
		};
		pageGrid.Add(new Label
		{
			Text = "Mixed-height CollectionView scrollbar",
			FontAttributes = FontAttributes.Bold,
			FontSize = 20
		});
		pageGrid.Add(new Label { Text = "Scroll from the short items into the tall items." }, 0, 1);
		pageGrid.Add(scrollState, 0, 2);
		pageGrid.Add(collection, 0, 3);

		BindingContext = items;
		Content = pageGrid;
	}

	sealed record VariableHeightItem(string Text, string AutomationId, double Height, Color Color);
}
#endif

