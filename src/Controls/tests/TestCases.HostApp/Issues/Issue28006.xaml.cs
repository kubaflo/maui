using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28006, "CollectionView scroll position changes when inserting an item", PlatformAffected.UWP)]
public partial class Issue28006 : ContentPage
{
	const int ReferenceItemIndex = 10;
	readonly ObservableCollection<GalleryItem> _items = new();
	bool _waitingForInitialScroll;
	bool _waitingForInsertionScroll;

	public Issue28006()
	{
		InitializeComponent();

		string[] captions =
		{
			"cover1.jpg",
			"oasis.jpg",
			"photo.jpg",
			"Vegetables.jpg",
			"Fruits.jpg",
			"FlowerBuds.jpg",
			"Legumes.jpg"
		};

		for (int index = 0; index < 20; index++)
		{
			_items.Add(new GalleryItem(
				$"{captions[index % captions.Length]}, {index}",
				"groceries.png",
				$"Item{index}",
				$"Item{index}Image"));
		}

		Items = _items;
		BindingContext = this;
	}

	public ObservableCollection<GalleryItem> Items { get; }

	void OnScrollToMiddleClicked(object sender, EventArgs e)
	{
		_waitingForInsertionScroll = false;
		_waitingForInitialScroll = true;
		ScrollTokenLabel.Text = "Token=-1";
		FirstVisibleIndexLabel.Text = $"FirstVisible=-1;Count={_items.Count}";
		ItemsCollection.ScrollTo(ReferenceItemIndex, position: ScrollToPosition.Start, animate: false);
	}

	void OnAddItemAboveClicked(object sender, EventArgs e)
	{
		_waitingForInsertionScroll = true;
		_items.Insert(ReferenceItemIndex - 1, new GalleryItem(
			"Inserted item",
			"groceries.png",
			"InsertedItem",
			"InsertedItemImage"));
	}

	void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (_waitingForInitialScroll && e.FirstVisibleItemIndex == ReferenceItemIndex)
		{
			_waitingForInitialScroll = false;
			FirstVisibleIndexLabel.Text = $"FirstVisible={e.FirstVisibleItemIndex};Count={_items.Count}";
			ScrollTokenLabel.Text = "Token=0";
			return;
		}

		if (_waitingForInsertionScroll)
		{
			_waitingForInsertionScroll = false;
			FirstVisibleIndexLabel.Text = $"FirstVisible={e.FirstVisibleItemIndex};Count={_items.Count}";
			ScrollTokenLabel.Text = "Token=1";
		}
	}

	public sealed class GalleryItem
	{
		public GalleryItem(string caption, string imageSource, string itemAutomationId, string imageAutomationId)
		{
			Caption = caption;
			ImageSource = imageSource;
			ItemAutomationId = itemAutomationId;
			ImageAutomationId = imageAutomationId;
		}

		public string Caption { get; }
		public string ImageSource { get; }
		public string ItemAutomationId { get; }
		public string ImageAutomationId { get; }
	}
}
