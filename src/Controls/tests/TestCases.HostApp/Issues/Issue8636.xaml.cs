using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 8636, "CollectionView size not updating", PlatformAffected.iOS)]
public partial class Issue8636 : ContentPage
{
	const double MeasurementTolerance = 0.5;

	int _mutationIndex = -1;
	bool _innerCollectionLoaded;
	double _initialInnerHeight = -1;
	CollectionView _innerCollection;

	public Issue8636()
	{
		InitializeComponent();

		OuterItems = new ObservableCollection<OuterItem>
		{
			new()
		};
		OuterItems[0].InnerItems.CollectionChanged += OnInnerItemsChanged;
		BindingContext = this;
	}

	public ObservableCollection<OuterItem> OuterItems { get; }

	void OnInnerCollectionLoaded(object sender, EventArgs e)
	{
		_innerCollectionLoaded = true;
		CaptureInitialHeight(sender);
		UpdateState();
	}

	void OnInnerCollectionSizeChanged(object sender, EventArgs e)
	{
		CaptureInitialHeight(sender);
		UpdateState();
	}

	void CaptureInitialHeight(object sender)
	{
		if (_mutationIndex < 0 && _initialInnerHeight < 0 && sender is CollectionView collectionView && collectionView.Height > 0)
		{
			_innerCollection = collectionView;
			_initialInnerHeight = collectionView.Height;
		}
	}

	void OnInnerItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		_mutationIndex = e.NewStartingIndex;
		UpdateState();
	}

	void OnGrowClicked(object sender, EventArgs e)
	{
		GrowButton.IsEnabled = false;
		OuterItems[0].InnerItems.Add("Row 2");
	}

	void OnCheckClicked(object sender, EventArgs e)
	{
		var currentHeight = _innerCollection?.Height ?? -1;
		var remeasured = _initialInnerHeight > 0 && currentHeight > _initialInnerHeight + MeasurementTolerance;
		ResultLabel.Text = $"checked=true;s={OuterItems[0].InnerItems.Count};m={_mutationIndex};remeasured={remeasured}";
		CheckButton.IsEnabled = false;
	}

	void UpdateState()
	{
		var sourceCount = OuterItems[0].InnerItems.Count;
		StateLabel.Text = $"s={sourceCount};m={_mutationIndex};loaded={_innerCollectionLoaded}";
		GrowButton.IsEnabled = _innerCollectionLoaded && _initialInnerHeight > 0 && _mutationIndex < 0;
		CheckButton.IsEnabled = _mutationIndex == 1 && sourceCount == 2;
	}

	public sealed class OuterItem
	{
		public ObservableCollection<string> InnerItems { get; } = new()
		{
			"Row 1"
		};
	}
}
