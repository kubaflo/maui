#if WINDOWS
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34754, "WinUI Drag and Drop and CanMixGroups support was not available", PlatformAffected.UWP)]
public class Issue34754 : ContentPage
{
	public Issue34754()
	{
		Title = "Grouped CollectionView Reordering";

		var alpha = new Issue34754Item("Alpha", "Issue34754Alpha");
		var firstGroup = new Issue34754Group("Group 1")
		{
			alpha,
			new Issue34754Item("Beta", "Issue34754Beta")
		};
		var secondGroup = new Issue34754Group("Group 2")
		{
			new Issue34754Item("Gamma", "Issue34754Gamma"),
			new Issue34754Item("Delta", "Issue34754Delta")
		};

		var firstStateLabel = new Label { AutomationId = "Issue34754Group1State" };
		var firstCountLabel = new Label { AutomationId = "Issue34754Group1Count" };
		var secondStateLabel = new Label { AutomationId = "Issue34754Group2State" };
		var secondCountLabel = new Label { AutomationId = "Issue34754Group2Count" };
		var pointerCountLabel = new Label
		{
			AutomationId = "Issue34754PointerCount",
			Text = "Alpha Pointer Count=0"
		};
		var handlerPathLabel = new Label
		{
			AutomationId = "Issue34754HandlerPath",
			Text = "Handler=not loaded"
		};
		var hierarchyLabel = new Label
		{
			AutomationId = "Issue34754Hierarchy",
			Text = "Hierarchy=ContentPage>Grid>CollectionView"
		};

		void UpdateSourceState()
		{
			firstStateLabel.Text = $"Group 1=[{string.Join(",", firstGroup.Select(item => item.Name))}]";
			firstCountLabel.Text = $"Group 1 Count={firstGroup.Count}";
			secondStateLabel.Text = $"Group 2=[{string.Join(",", secondGroup.Select(item => item.Name))}]";
			secondCountLabel.Text = $"Group 2 Count={secondGroup.Count}";
		}

		UpdateSourceState();
		firstGroup.CollectionChanged += (_, _) => UpdateSourceState();
		secondGroup.CollectionChanged += (_, _) => UpdateSourceState();

		int alphaPointerCount = 0;
		var groupedCollection = new CollectionView
		{
			AutomationId = "Issue34754CollectionView",
			IsGrouped = true,
			CanReorderItems = true,
			CanMixGroups = true,
			ItemsSource = new ObservableCollection<Issue34754Group> { firstGroup, secondGroup },
			GroupHeaderTemplate = new DataTemplate(() =>
			{
				var header = new Label
				{
					BackgroundColor = Colors.LightGray,
					FontAttributes = FontAttributes.Bold,
					Padding = new Thickness(12, 8)
				};
				header.SetBinding(Label.TextProperty, nameof(Issue34754Group.GroupName));
				return header;
			}),
			ItemTemplate = new DataTemplate(() =>
			{
				var itemLabel = new Label
				{
					HeightRequest = 52,
					Padding = 12,
					VerticalTextAlignment = TextAlignment.Center
				};
				itemLabel.SetBinding(Label.TextProperty, nameof(Issue34754Item.Name));
				itemLabel.SetBinding(AutomationIdProperty, nameof(Issue34754Item.AutomationId));

				var pointerRecognizer = new PointerGestureRecognizer();
				pointerRecognizer.PointerPressed += (_, _) =>
				{
					if (pointerRecognizer.BindingContext is Issue34754Item { Name: "Alpha" })
					{
						alphaPointerCount++;
						pointerCountLabel.Text = $"Alpha Pointer Count={alphaPointerCount}";
					}
				};
				itemLabel.GestureRecognizers.Add(pointerRecognizer);

				return new Border
				{
					Margin = new Thickness(0, 2),
					Padding = 4,
					Stroke = Colors.DarkGray,
					StrokeThickness = 1,
					Content = itemLabel
				};
			})
		};
		groupedCollection.Loaded += (_, _) =>
			handlerPathLabel.Text = $"Handler={groupedCollection.Handler?.GetType().FullName ?? "null"}";

		var firstStateRow = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			}
		};
		firstStateRow.Add(firstStateLabel);
		firstStateRow.Add(firstCountLabel, 1);

		var secondStateRow = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			}
		};
		secondStateRow.Add(secondStateLabel);
		secondStateRow.Add(secondCountLabel, 1);

		var diagnostics = new HorizontalStackLayout
		{
			Spacing = 24,
			Children = { pointerCountLabel, handlerPathLabel, hierarchyLabel }
		};

		var rootGrid = new Grid
		{
			AutomationId = "Issue34754WindowContent",
			Padding = 24,
			RowSpacing = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		rootGrid.Add(new Label
		{
			Text = "Drag Alpha from Group 1 onto Gamma in Group 2",
			FontAttributes = FontAttributes.Bold,
			FontSize = 20
		});
		rootGrid.Add(firstStateRow, 0, 1);
		rootGrid.Add(secondStateRow, 0, 2);
		rootGrid.Add(groupedCollection, 0, 3);
		rootGrid.Add(diagnostics, 0, 4);
		Content = rootGrid;
	}
}

sealed class Issue34754Group : ObservableCollection<Issue34754Item>
{
	public Issue34754Group(string groupName)
	{
		GroupName = groupName;
	}

	public string GroupName { get; }
}

sealed class Issue34754Item
{
	public Issue34754Item(string name, string automationId)
	{
		Name = name;
		AutomationId = automationId;
	}

	public string Name { get; }
	public string AutomationId { get; }
}
#endif

