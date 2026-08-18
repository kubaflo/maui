using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35617, "Horizontal CollectionView delays rendering the first rapidly added item", PlatformAffected.UWP)]
public partial class Issue35617 : ContentPage
{
	readonly ObservableCollection<string> _items = new();
	readonly List<string> _inspectionRecords = new();
	int _callbackCount;
	int _cycle;
	int _inspectionIndex = -1;

	public Issue35617()
	{
		InitializeComponent();
		ItemsCollectionView.ItemsSource = _items;
		ResetCollection();
	}

	void ItemsCollectionView_Loaded(object sender, EventArgs e)
	{
		UpdateResult();
	}

	void AddButton_Clicked(object sender, EventArgs e)
	{
		_ = ItemsCollectionView.DesiredSize.Height;

		int itemIndex = _items.Count;
		string expectedText = $"item: {itemIndex}";
		int cycle = _cycle;
		_items.Add(expectedText);

#if WINDOWS
		Dispatcher.Dispatch(() => InspectRenderedItem(itemIndex, expectedText, cycle));
#endif
	}

	void ResetButton_Clicked(object sender, EventArgs e)
	{
		ResetCollection();
	}

	void ResetCollection()
	{
		_cycle++;
		_callbackCount = 0;
		_inspectionIndex = -1;
		_inspectionRecords.Clear();
		_items.Clear();
		_items.Add("item: 0");
		UpdateResult();
	}

	void UpdateResult()
	{
#if WINDOWS
		bool handlerAttached = ItemsCollectionView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ListViewBase;
#else
		bool handlerAttached = false;
#endif
		ResultLabel.Text = $"cycle={_cycle};callbacks={_callbackCount};index={_inspectionIndex};handler={handlerAttached}|{string.Join("|", _inspectionRecords)}";
	}

#if WINDOWS
	void InspectRenderedItem(int itemIndex, string expectedText, int cycle)
	{
		if (cycle != _cycle)
			return;

		_callbackCount++;
		_inspectionIndex = itemIndex;

		var list = ItemsCollectionView.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.ListViewBase;
		var container = list?.ContainerFromIndex(itemIndex) as Microsoft.UI.Xaml.FrameworkElement;
		var textBlock = container is null ? null : FindTextBlock(container, expectedText);

		bool sourceMatch = itemIndex < _items.Count && _items[itemIndex] == expectedText;
		double width = container?.ActualWidth ?? 0;
		double height = container?.ActualHeight ?? 0;
		bool visible = container?.Visibility == Microsoft.UI.Xaml.Visibility.Visible;
		double textWidth = textBlock?.ActualWidth ?? 0;
		double textHeight = textBlock?.ActualHeight ?? 0;
		bool textVisible = textBlock?.Visibility == Microsoft.UI.Xaml.Visibility.Visible;
		bool rendered = sourceMatch &&
			container is not null &&
			width > 0.5 &&
			height > 0.5 &&
			visible &&
			textBlock is not null &&
			textWidth > 0.5 &&
			textHeight > 0.5 &&
			textVisible;

		int containerId = container is null ? -1 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(container);
		_inspectionRecords.Add(FormattableString.Invariant(
			$"{expectedText};index={itemIndex};source={sourceMatch};rendered={rendered};containerId={containerId};width={width:F2};height={height:F2};visible={visible};text={textBlock is not null};textWidth={textWidth:F2};textHeight={textHeight:F2};textVisible={textVisible}"));

		if (_callbackCount == 4)
			UpdateResult();
	}

	static Microsoft.UI.Xaml.Controls.TextBlock FindTextBlock(Microsoft.UI.Xaml.DependencyObject element, string expectedText)
	{
		if (element is Microsoft.UI.Xaml.Controls.TextBlock textBlock && textBlock.Text == expectedText)
			return textBlock;

		int childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
		for (int index = 0; index < childCount; index++)
		{
			var match = FindTextBlock(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, index), expectedText);
			if (match is not null)
				return match;
		}

		return null;
	}
#endif
}
