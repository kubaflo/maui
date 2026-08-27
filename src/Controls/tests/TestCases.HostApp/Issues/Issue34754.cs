#if WINDOWS
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34754, "WinUI drag and drop and CanMixGroups support was not available", PlatformAffected.UWP)]
public class Issue34754 : ContentPage
{
	readonly Issue34754Group _groupA;
	readonly Issue34754Group _groupB;
	readonly Issue34754Item _alphaOne;
	readonly Label _selectionStateLabel;
	readonly Label _transitionStateLabel;
	int _collectionCallback = -1;

	public Issue34754()
	{
		_alphaOne = new Issue34754Item("Alpha one", "AlphaOne");
		_groupA = new Issue34754Group("Group A", "GroupAHeader")
		{
			_alphaOne,
			new Issue34754Item("Alpha two", "AlphaTwo")
		};
		_groupB = new Issue34754Group("Group B", "GroupBHeader")
		{
			new Issue34754Item("Beta one", "BetaOne"),
			new Issue34754Item("Beta two", "BetaTwo")
		};

		_selectionStateLabel = new Label
		{
			AutomationId = "SelectionState",
			Text = "Selected=<none>"
		};
		_transitionStateLabel = new Label
		{
			AutomationId = "TransitionState",
			Text = GetTransitionState()
		};

		var collectionView = new CollectionView
		{
			AutomationId = "ItemsCollection",
			IsGrouped = true,
			CanReorderItems = true,
			CanMixGroups = true,
			SelectionMode = SelectionMode.Single,
			ItemsSource = new ObservableCollection<Issue34754Group> { _groupA, _groupB },
			GroupHeaderTemplate = CreateGroupHeaderTemplate(),
			ItemTemplate = CreateItemTemplate()
		};
		collectionView.SelectionChanged += (_, e) =>
		{
			if (e.CurrentSelection.Count > 0 && e.CurrentSelection[0] is Issue34754Item item)
				_selectionStateLabel.Text = $"Selected={item.Name}";
		};

		_groupA.CollectionChanged += (_, _) => UpdateTransitionState();
		_groupB.CollectionChanged += (_, _) => UpdateTransitionState();

		var propertyStateLabel = new Label
		{
			AutomationId = "PropertyState",
			Text = $"CanReorderItems={collectionView.CanReorderItems};CanMixGroups={collectionView.CanMixGroups}"
		};

		var rootGrid = new Grid
		{
			AutomationId = "Issue34754Root",
			Padding = 20,
			RowSpacing = 10,
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
			FontAttributes = FontAttributes.Bold,
			FontSize = 22,
			Text = "Windows CollectionView grouped reordering"
		}, 0, 0);
		rootGrid.Add(new Label
		{
			Text = "Select Alpha one, then drag it into Group B. CanReorderItems and CanMixGroups are enabled."
		}, 0, 1);
		rootGrid.Add(propertyStateLabel, 0, 2);
		rootGrid.Add(collectionView, 0, 3);
		rootGrid.Add(new VerticalStackLayout
		{
			Spacing = 8,
			Children =
			{
				_selectionStateLabel,
				_transitionStateLabel
			}
		}, 0, 4);

		Content = rootGrid;
	}

	static DataTemplate CreateGroupHeaderTemplate()
	{
		return new DataTemplate(() =>
		{
			var headerLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 18
			};
			headerLabel.SetBinding(Label.TextProperty, nameof(Issue34754Group.Name));
			headerLabel.SetBinding(AutomationIdProperty, nameof(Issue34754Group.HeaderAutomationId));

			return new Grid
			{
				BackgroundColor = Color.FromArgb("#E5E7EB"),
				Padding = new Thickness(10, 6),
				Children = { headerLabel }
			};
		});
	}

	static DataTemplate CreateItemTemplate()
	{
		return new DataTemplate(() =>
		{
			var itemLabel = new Label
			{
				FontSize = 18
			};
			itemLabel.SetBinding(Label.TextProperty, nameof(Issue34754Item.Name));
			itemLabel.SetBinding(AutomationIdProperty, nameof(Issue34754Item.AutomationId));

			return new Grid
			{
				Margin = new Thickness(0, 3),
				Padding = 14,
				BackgroundColor = Color.FromArgb("#DDEEFF"),
				MinimumHeightRequest = 56,
				Children = { itemLabel }
			};
		});
	}

	void UpdateTransitionState()
	{
		_collectionCallback++;
		_transitionStateLabel.Text = GetTransitionState();
	}

	string GetTransitionState()
	{
		var group = _groupB.Contains(_alphaOne) ? "B" : _groupA.Contains(_alphaOne) ? "A" : "None";
		return $"Callback={_collectionCallback};Group={group};TransitionObserved={_collectionCallback >= 0}";
	}
}

sealed class Issue34754Group : ObservableCollection<Issue34754Item>
{
	public Issue34754Group(string name, string headerAutomationId)
	{
		Name = name;
		HeaderAutomationId = headerAutomationId;
	}

	public string Name { get; }

	public string HeaderAutomationId { get; }
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

