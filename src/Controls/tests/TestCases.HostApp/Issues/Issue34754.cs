#if WINDOWS
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34754, "WinUI drag and drop and CanMixGroups support was not available", PlatformAffected.UWP)]
public class Issue34754 : ContentPage
{
	public Issue34754()
	{
		var alpha = new Issue34754Item("Alpha", "Issue34754Alpha");
		var groupOne = new Issue34754Group("Group One", [alpha, new Issue34754Item("Beta", "Issue34754Beta")]);
		var groupTwo = new Issue34754Group("Group Two", [new Issue34754Item("Gamma", "Issue34754Gamma"), new Issue34754Item("Delta", "Issue34754Delta")]);
		var inputStatusLabel = new Label
		{
			AutomationId = "Issue34754InputStatus",
			Text = "WAITING: Alpha not tapped",
			VerticalTextAlignment = TextAlignment.Center
		};
		var collectionChangeLabel = new Label
		{
			AutomationId = "Issue34754CollectionChangeCount",
			Text = "Collection changes: -1"
		};
		var groupOneSequenceLabel = new Label
		{
			AutomationId = "Issue34754GroupOneSequence",
			Text = GetSequence(groupOne)
		};
		var groupTwoSequenceLabel = new Label
		{
			AutomationId = "Issue34754GroupTwoSequence",
			Text = GetSequence(groupTwo)
		};
		var collectionChangeCount = -1;

		void OnGroupChanged()
		{
			collectionChangeCount++;
			collectionChangeLabel.Text = $"Collection changes: {collectionChangeCount}; post-drag callback observed";
			groupOneSequenceLabel.Text = GetSequence(groupOne);
			groupTwoSequenceLabel.Text = GetSequence(groupTwo);
		}

		groupOne.CollectionChanged += (_, _) => OnGroupChanged();
		groupTwo.CollectionChanged += (_, _) => OnGroupChanged();

		var collectionView = new CollectionView
		{
			AutomationId = "Issue34754GroupedCollectionView",
			IsGrouped = true,
			CanReorderItems = true,
			CanMixGroups = true,
			SelectionMode = SelectionMode.None,
			ItemsSource = new ObservableCollection<Issue34754Group> { groupOne, groupTwo },
			GroupHeaderTemplate = new DataTemplate(() =>
			{
				var header = new Label
				{
					BackgroundColor = Colors.LightBlue,
					FontAttributes = FontAttributes.Bold,
					FontSize = 18,
					Padding = 8
				};
				header.SetBinding(Label.TextProperty, nameof(Issue34754Group.Name));
				return header;
			}),
			ItemTemplate = new DataTemplate(() =>
			{
				var itemGrid = new Grid
				{
					BackgroundColor = Colors.LightGray,
					Margin = 4,
					Padding = 18,
					MinimumHeightRequest = 64
				};
				itemGrid.SetBinding(AutomationIdProperty, nameof(Issue34754Item.AutomationId));

				var itemLabel = new Label
				{
					FontSize = 17,
					VerticalOptions = LayoutOptions.Center
				};
				itemLabel.SetBinding(Label.TextProperty, nameof(Issue34754Item.Name));
				itemGrid.Add(itemLabel);

				var tapGestureRecognizer = new TapGestureRecognizer();
				tapGestureRecognizer.SetBinding(TapGestureRecognizer.CommandParameterProperty, ".");
				tapGestureRecognizer.Tapped += (sender, args) =>
				{
					if (args.Parameter == alpha)
						inputStatusLabel.Text = "INPUT CONFIRMED: Alpha tapped";
				};
				itemGrid.GestureRecognizers.Add(tapGestureRecognizer);

				return itemGrid;
			})
		};

		var grid = new Grid
		{
			Padding = 24,
			RowSpacing = 10,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		grid.Add(new Label
		{
			Text = "Windows CollectionView grouped drag and drop",
			FontSize = 22,
			FontAttributes = FontAttributes.Bold
		});
		grid.Add(new Label
		{
			Text = "CanReorderItems=True, CanMixGroups=True. Drag Alpha from Group One into Group Two."
		}, 0, 1);
		grid.Add(new HorizontalStackLayout
		{
			Spacing = 18,
			Children = { inputStatusLabel, collectionChangeLabel }
		}, 0, 2);
		grid.Add(new VerticalStackLayout
		{
			Children = { groupOneSequenceLabel, groupTwoSequenceLabel }
		}, 0, 3);
		grid.Add(collectionView, 0, 4);
		Content = grid;
	}

	static string GetSequence(Issue34754Group group) =>
		$"{group.Name}: {string.Join(",", group.Select(item => item.Name))}";
}

sealed class Issue34754Group(string name, IEnumerable<Issue34754Item> items) : ObservableCollection<Issue34754Item>(items)
{
	public string Name { get; } = name;
}

sealed class Issue34754Item(string name, string automationId)
{
	public string Name { get; } = name;
	public string AutomationId { get; } = automationId;
}
#endif

