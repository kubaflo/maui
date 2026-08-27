namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26449, "Unable to scroll inner CollectionView of nested CollectionViews", PlatformAffected.Android)]
public class Issue26449 : TestContentPage
{
	protected override void Init()
	{
		var innerOffsetLabel = new Label
		{
			AutomationId = "Issue26449InnerOffset",
			Text = "-1"
		};
		var outerOffsetLabel = new Label
		{
			AutomationId = "Issue26449OuterOffset",
			Text = "-1"
		};
		var callbackLabel = new Label
		{
			AutomationId = "Issue26449Callback",
			Text = "Waiting"
		};
		var armed = false;
		var groups = CreateGroups();

		var outerCollection = new CollectionView
		{
			SelectionMode = SelectionMode.None,
			ItemsSource = groups,
			ItemTemplate = new DataTemplate(() =>
			{
				var titleLabel = new Label
				{
					FontAttributes = FontAttributes.Bold
				};
				titleLabel.SetBinding(Label.TextProperty, nameof(CollectionGroup.Title));

				var innerCollection = new CollectionView
				{
					HeightRequest = 240,
					SelectionMode = SelectionMode.None,
					ItemTemplate = new DataTemplate(() =>
					{
						var itemLabel = new Label
						{
							HeightRequest = 44,
							VerticalTextAlignment = TextAlignment.Center
						};
						itemLabel.SetBinding(Label.TextProperty, Binding.SelfPath);
						return itemLabel;
					})
				};
				innerCollection.SetBinding(ItemsView.ItemsSourceProperty, nameof(CollectionGroup.Items));
				innerCollection.Scrolled += (_, args) =>
				{
					if (armed &&
						innerCollection.BindingContext is CollectionGroup { Title: "Outer group 1" } &&
						args.VerticalOffset > 1)
					{
						innerOffsetLabel.Text = args.VerticalOffset.ToString(System.Globalization.CultureInfo.InvariantCulture);
						callbackLabel.Text = "Observed";
					}
				};

				return new VerticalStackLayout
				{
					Spacing = 4,
					Children =
					{
						titleLabel,
						innerCollection
					}
				};
			})
		};
		outerCollection.Scrolled += (_, args) =>
		{
			if (armed && args.VerticalOffset > 1)
			{
				outerOffsetLabel.Text = args.VerticalOffset.ToString(System.Globalization.CultureInfo.InvariantCulture);
				callbackLabel.Text = "Observed";
			}
		};

		var prepareButton = new Button
		{
			AutomationId = "Issue26449Prepare",
			Text = "Prepare scroll check"
		};
		prepareButton.Clicked += (_, _) =>
		{
			armed = true;
			innerOffsetLabel.Text = "-1";
			outerOffsetLabel.Text = "-1";
			callbackLabel.Text = "Waiting";
		};

		var sourceLabel = new Label
		{
			AutomationId = "Issue26449Ready",
			Text = $"Groups={groups.Length};FirstInnerItems={groups[0].Items.Length}"
		};

		var telemetryLayout = new HorizontalStackLayout
		{
			Children =
			{
				new Label { Text = "Inner:" },
				innerOffsetLabel,
				new Label { Text = "Outer:" },
				outerOffsetLabel,
				callbackLabel
			}
		};

		var rootGrid = new Grid
		{
			Padding = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 6
		};
		rootGrid.Add(new Label
		{
			Text = "Drag upward on an Inner 1 item. The inner list should scroll, not the outer list.",
			FontAttributes = FontAttributes.Bold
		});
		rootGrid.Add(sourceLabel, row: 1);
		rootGrid.Add(telemetryLayout, row: 2);
		rootGrid.Add(prepareButton, row: 3);
		rootGrid.Add(outerCollection, row: 4);
		Content = rootGrid;
	}

	static CollectionGroup[] CreateGroups()
	{
		var groups = new CollectionGroup[4];

		for (var groupIndex = 0; groupIndex < groups.Length; groupIndex++)
		{
			var items = new string[20];

			for (var itemIndex = 0; itemIndex < items.Length; itemIndex++)
				items[itemIndex] = $"Inner {groupIndex + 1} item {itemIndex + 1}";

			groups[groupIndex] = new CollectionGroup($"Outer group {groupIndex + 1}", items);
		}

		return groups;
	}

	sealed class CollectionGroup
	{
		public CollectionGroup(string title, string[] items)
		{
			Title = title;
			Items = items;
		}

		public string Title { get; }

		public string[] Items { get; }
	}
}

