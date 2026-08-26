#if WINDOWS
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34754, "WinUI drag and drop and CanMixGroups support was not available", PlatformAffected.UWP)]
public class Issue34754 : ContentPage
{
	public Issue34754()
	{
		Title = "CollectionView group reordering";

		var sourceItem = new Issue34754Item("SOURCE ITEM", "SourceItem", Colors.LightBlue);
		var targetItem = new Issue34754Item("TARGET ITEM", "TargetItem", Colors.LightGreen);
		var groupA = new Issue34754Group("Group A", sourceItem);
		var groupB = new Issue34754Group("Group B", targetItem);
		ObservableCollection<Issue34754Group> groups = [groupA, groupB];

		var collectionView = new CollectionView
		{
			ItemsSource = groups,
			IsGrouped = true,
			CanReorderItems = true,
			CanMixGroups = true,
			SelectionMode = SelectionMode.None,
			GroupHeaderTemplate = CreateGroupHeaderTemplate(),
			ItemTemplate = CreateItemTemplate()
		};

		var grid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(360),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			RowSpacing = 12
		};

		var instructions = new Label
		{
			Text = "Drag SOURCE ITEM from Group A onto TARGET ITEM in Group B.",
			FontSize = 18
		};

		grid.Add(instructions);
		grid.Add(collectionView, 0, 1);
		Content = grid;
	}

	DataTemplate CreateGroupHeaderTemplate()
	{
		return new DataTemplate(() =>
		{
			var label = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 18,
				Padding = 8,
				BackgroundColor = Colors.LightGray
			};
			label.SetBinding(Label.TextProperty, nameof(Issue34754Group.Name));
			return label;
		});
	}

	DataTemplate CreateItemTemplate()
	{
		return new DataTemplate(() =>
		{
			var label = new Label
			{
				FontSize = 18,
				HeightRequest = 72,
				Padding = 18,
				Margin = 2,
				VerticalTextAlignment = TextAlignment.Center
			};
			label.SetBinding(Label.TextProperty, nameof(Issue34754Item.Text));
			label.SetBinding(AutomationIdProperty, nameof(Issue34754Item.AutomationId));
			label.SetBinding(Label.BackgroundColorProperty, nameof(Issue34754Item.BackgroundColor));
			return label;
		});
	}
}

public sealed class Issue34754Item(string text, string automationId, Color backgroundColor)
{
	public string Text { get; } = text;
	public string AutomationId { get; } = automationId;
	public Color BackgroundColor { get; } = backgroundColor;
}

public sealed class Issue34754Group(string name, params Issue34754Item[] items) : ObservableCollection<Issue34754Item>(items)
{
	public string Name { get; } = name;
}
#endif

