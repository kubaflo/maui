namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35770, "Nested CollectionView does not scroll on Android", PlatformAffected.Android)]
public partial class Issue35770 : ContentPage
{
	const int ItemHeight = 64;
	const int ItemSpacing = 8;

	int _innerScrollCallbackCount;
	double _verticalOffset = -1;
	double _verticalDelta;
	int _sourceCount = -1;
	double _viewportHeight = -1;
	double _contentExtent = -1;

	public Issue35770()
	{
		InitializeComponent();

		OuterCollectionView.ItemsSource = new[]
		{
			new NestedRow("Outer row 1", "Row 1 Item 1", "Row 1 Item 2", "Row 1 Item 3", "Row 1 Item 4", "Row 1 Item 5"),
			new NestedRow("Outer row 2", "Row 2 Item 1", "Row 2 Item 2", "Row 2 Item 3", "Row 2 Item 4", "Row 2 Item 5"),
			new NestedRow("Outer row 3", "Row 3 Item 1", "Row 3 Item 2", "Row 3 Item 3", "Row 3 Item 4", "Row 3 Item 5")
		};
	}

	void OnInnerCollectionSizeChanged(object sender, EventArgs e)
	{
		if (sender is not CollectionView { BindingContext: NestedRow { Title: "Outer row 1" } row } innerCollection
			|| innerCollection.Height <= 0)
		{
			return;
		}

		_sourceCount = row.Items.Length;
		_viewportHeight = Math.Round(innerCollection.Height);
		_contentExtent = (_sourceCount * ItemHeight) + ((_sourceCount - 1) * ItemSpacing);
		UpdateResult("ready");
	}

	void OnInnerCollectionScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (sender is CollectionView { BindingContext: NestedRow { Title: "Outer row 1" } }
			&& (e.VerticalOffset > 0 || e.VerticalDelta != 0))
		{
			_innerScrollCallbackCount++;
			_verticalOffset = e.VerticalOffset;
			_verticalDelta = e.VerticalDelta;
			UpdateResult("ready");
		}
	}

	void OnCheckClicked(object sender, EventArgs e)
	{
		UpdateResult("checked");
	}

	void UpdateResult(string state)
	{
		ResultLabel.Text = FormattableString.Invariant(
			$"{_innerScrollCallbackCount}|{_verticalOffset}|{_verticalDelta}|{_sourceCount}|{_viewportHeight}|{_contentExtent}|{state}");
	}

	sealed class NestedRow
	{
		public NestedRow(string title, params string[] items)
		{
			Title = title;
			Items = items;
		}

		public string Title { get; }

		public string[] Items { get; }
	}
}
