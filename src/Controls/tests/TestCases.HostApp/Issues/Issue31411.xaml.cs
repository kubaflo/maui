using System.Collections.ObjectModel;

#if WINDOWS
using WListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31411, "Poor CollectionView performance, ghosting, and crashing on Windows", PlatformAffected.UWP)]
public partial class Issue31411 : ContentPage
{
	int _completedCycle = -1;
	int _cycle;
	int _firstVisibleItem = -1;
	int _pendingCycle = -1;

	public Issue31411()
	{
		InitializeComponent();
		Items = new ObservableCollection<CollectionItem>(
			Enumerable.Range(0, 2000).Select(index => new CollectionItem(index)));
		BindingContext = this;

#if WINDOWS
		ItemsCollection.HandlerChanged += OnCollectionHandlerChanged;
#endif
	}

	public ObservableCollection<CollectionItem> Items { get; }

	void OnBulkUpdateClicked(object sender, EventArgs e)
	{
		_cycle++;
		_pendingCycle = _cycle;
		bool hideEvenItems = (_cycle % 2) != 0;

		for (int index = 0; index < Items.Count; index += 2)
			Items[index].IsVisible = !hideEvenItems;
	}

	void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		_firstVisibleItem = e.FirstVisibleItemIndex;
		UpdateStatus();
	}

	void UpdateStatus()
	{
		StatusLabel.Text = $"Ready:{Items.Count};Cycle:{_completedCycle};FirstVisible:{_firstVisibleItem}";
	}

#if WINDOWS
	void OnCollectionHandlerChanged(object sender, EventArgs e)
	{
		if (ItemsCollection.Handler?.PlatformView is WListViewBase nativeList)
			nativeList.LayoutUpdated += (_, _) => OnNativeLayoutUpdated();
	}

	void OnNativeLayoutUpdated()
	{
		if (_pendingCycle <= _completedCycle)
			return;

		_completedCycle = _pendingCycle;
		UpdateStatus();
	}
#endif

	public sealed class CollectionItem : BindableObject
	{
		public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
			nameof(IsVisible),
			typeof(bool),
			typeof(CollectionItem),
			true);

		public CollectionItem(int index)
		{
			Index = index;
			ItemSemanticDescription = $"Issue31411Item{(index % 2 == 0 ? "Even" : "Odd")}{index:D4}";
			Title = $"Virtualized item {index}";
			Detail = $"Complex bound content for item {index} during repeated visibility updates";
			Category = $"Group {index % 7}";
			Progress = (index % 100) / 100d;
			IsActive = (index % 3) == 0;
		}

		public int Index { get; }

		public string ItemSemanticDescription { get; }

		public string Title { get; }

		public string Detail { get; }

		public string Category { get; }

		public double Progress { get; }

		public bool IsActive { get; }

		public bool IsVisible
		{
			get => (bool)GetValue(IsVisibleProperty);
			set => SetValue(IsVisibleProperty, value);
		}
	}
}
