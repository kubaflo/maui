namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28542, "CollectionView scrollbar has inconsistent sizing with variable-height items", PlatformAffected.Android)]
public class Issue28542 : TestContentPage
{
	protected override void Init()
	{
		Title = "CollectionView scrollbar";

		var instructionLabel = new Label
		{
			Text = "Scroll from short items into tall items and watch the scrollbar thumb.",
			FontAttributes = FontAttributes.Bold,
			AutomationId = "InstructionLabel"
		};

		var resultLabel = new Label
		{
			Text = "WaitingForScroll",
			FontAttributes = FontAttributes.Bold,
			FontSize = 18,
			AutomationId = "ScrollResult"
		};

		var itemsView = new CollectionView
		{
			AutomationId = "ItemsView",
			ItemTemplate = new DataTemplate(() =>
			{
				var itemLabel = new Label
				{
					FontSize = 20,
					TextColor = Colors.Black,
					VerticalOptions = LayoutOptions.Center
				};
				itemLabel.SetBinding(Label.TextProperty, nameof(ScrollItem.Text));

				var itemGrid = new Grid
				{
					Margin = new Thickness(0, 2),
					Padding = 16,
					BackgroundColor = Colors.LightGray
				};
				itemGrid.SetBinding(HeightRequestProperty, nameof(ScrollItem.Height));
				itemGrid.SetBinding(AutomationIdProperty, nameof(ScrollItem.Description));
				itemGrid.Add(itemLabel);
				return itemGrid;
			})
		};

		var firstRange = -1;
		var callbackCount = 0;
		itemsView.Scrolled += (_, args) =>
		{
			if (args.VerticalDelta == 0)
				return;

#if ANDROID
			if (itemsView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recyclerView)
			{
				var currentRange = recyclerView.ComputeVerticalScrollRange();
				if (callbackCount == 0)
					firstRange = currentRange;

				callbackCount++;
				resultLabel.Text = $"F:{firstRange};C:{currentRange};N:{callbackCount}";
			}
#endif
		};

		itemsView.ItemsSource = CreateRecordedItems();

		var rootGrid = new Grid
		{
			Padding = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			},
			RowSpacing = 8
		};
		rootGrid.Add(instructionLabel);
		rootGrid.Add(itemsView, 0, 1);
		rootGrid.Add(resultLabel, 0, 2);
		Content = rootGrid;
	}

	static List<ScrollItem> CreateRecordedItems()
	{
		var items = new List<ScrollItem>();
		for (var index = 1; index <= 8; index++)
			items.Add(new ScrollItem($"Short item {index}", 72, $"ShortItem{index}"));
		for (var index = 9; index <= 12; index++)
			items.Add(new ScrollItem($"Tall item {index}", 280, $"TallItem{index}"));
		for (var index = 13; index <= 20; index++)
			items.Add(new ScrollItem($"Short item {index}", 72, $"ShortItem{index}"));
		return items;
	}

	sealed record ScrollItem(string Text, double Height, string Description);
}

