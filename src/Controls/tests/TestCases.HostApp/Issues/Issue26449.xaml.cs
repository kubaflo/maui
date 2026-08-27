namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26449, "Unable to scroll inner CollectionView of nested CollectionViews", PlatformAffected.Android)]
public partial class Issue26449 : ContentPage
{
	int _sequence = -1;
	int _innerEvents;
	int _outerEvents;
	double _innerDelta;
	double _outerDelta;

	public Issue26449()
	{
		InitializeComponent();
		OuterCollection.ItemsSource = CreateGroups();
	}

	static IReadOnlyList<ItemGroup> CreateGroups()
	{
		var groups = new List<ItemGroup>();

		for (int groupIndex = 1; groupIndex <= 6; groupIndex++)
		{
			var items = new List<string>();

			for (int itemIndex = 1; itemIndex <= 20; itemIndex++)
				items.Add($"Group {groupIndex}, inner item {itemIndex}");

			groups.Add(new ItemGroup(
				$"Outer item {groupIndex}",
				items));
		}

		return groups;
	}

	void OnInnerCollectionScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (e.VerticalDelta == 0)
			return;

		_innerEvents++;
		_innerDelta += e.VerticalDelta;
		UpdateTelemetry("Inner");
	}

	void OnOuterCollectionScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (e.VerticalDelta == 0)
			return;

		_outerEvents++;
		_outerDelta += e.VerticalDelta;
		UpdateTelemetry("Outer");
	}

	void UpdateTelemetry(string source)
	{
		_sequence++;
		ScrollTelemetryLabel.Text =
			$"Source={source}; Sequence={_sequence}; InnerEvents={_innerEvents}; OuterEvents={_outerEvents}; InnerDelta={_innerDelta}; OuterDelta={_outerDelta}; CallbackObserved=True";
	}

	sealed class ItemGroup
	{
		public ItemGroup(string title, IReadOnlyList<string> items)
		{
			Title = title;
			Items = items;
		}

		public string Title { get; }

		public IReadOnlyList<string> Items { get; }
	}
}
