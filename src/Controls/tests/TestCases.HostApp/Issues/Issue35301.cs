using System.Globalization;
#if WINDOWS
using WBorder = Microsoft.UI.Xaml.Controls.Border;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WCornerRadius = Microsoft.UI.Xaml.CornerRadius;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WListView = Microsoft.UI.Xaml.Controls.ListView;
using WListViewItem = Microsoft.UI.Xaml.Controls.ListViewItem;
using WListViewItemPresenter = Microsoft.UI.Xaml.Controls.Primitives.ListViewItemPresenter;
using WRectangle = Microsoft.UI.Xaml.Shapes.Rectangle;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;
using WVisibility = Microsoft.UI.Xaml.Visibility;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35301, "Windows CollectionView applies WinUI styling on default", PlatformAffected.UWP)]
public class Issue35301 : ContentPage
{
	readonly CollectionView _fruitsCollectionView;
	readonly Label _initialMeasurementLabel;
	readonly Label _currentMeasurementLabel;
	int _selectedIndex = -1;
	int _inspectionSequence;
#if WINDOWS
	bool _initialMeasurementCaptured;
#endif

	public Issue35301()
	{
		var instructionLabel = new Label
		{
			Text = "Select Apple in the default CollectionView.",
			FontSize = 20
		};

		var selectionIndexLabel = new Label
		{
			AutomationId = "SelectionIndexLabel",
			Text = "-1"
		};

		_initialMeasurementLabel = new Label
		{
			AutomationId = "InitialMeasurementLabel",
			Text = "Pending"
		};

		_currentMeasurementLabel = new Label
		{
			AutomationId = "CurrentMeasurementLabel",
			Text = "Not inspected"
		};

		var inspectionSequenceLabel = new Label
		{
			AutomationId = "InspectionSequenceLabel",
			Text = "0"
		};

		var inspectButton = new Button
		{
			AutomationId = "CheckStyleButton",
			Text = "Check selected styling"
		};

		inspectButton.Clicked += (sender, args) =>
		{
			_inspectionSequence++;
			inspectionSequenceLabel.Text = _inspectionSequence.ToString(CultureInfo.InvariantCulture);
#if WINDOWS
			CaptureMeasurement(_currentMeasurementLabel, _inspectionSequence);
#endif
		};

		_fruitsCollectionView = new CollectionView
		{
			AutomationId = "FruitsCollectionView",
			SelectionMode = SelectionMode.Single,
			ItemsSource = new[] { "Apple", "Banana", "Cherry" },
			ItemTemplate = new DataTemplate(() =>
			{
				var fruitLabel = new Label
				{
					FontSize = 22,
					Padding = 8
				};
				fruitLabel.SetBinding(Label.TextProperty, ".");
#if WINDOWS
				fruitLabel.Loaded += (sender, args) => Dispatcher.Dispatch(TryCaptureInitialMeasurement);
#endif
				return fruitLabel;
			})
		};

		_fruitsCollectionView.SelectionChanged += (sender, args) =>
		{
			if (args.CurrentSelection.FirstOrDefault() is string selectedFruit)
				_selectedIndex = Array.IndexOf((string[])_fruitsCollectionView.ItemsSource, selectedFruit);

			selectionIndexLabel.Text = _selectedIndex.ToString(CultureInfo.InvariantCulture);
		};

		var statusLayout = new VerticalStackLayout
		{
			Spacing = 6,
			Children =
			{
				selectionIndexLabel,
				_initialMeasurementLabel,
				_currentMeasurementLabel,
				inspectionSequenceLabel,
				inspectButton
			}
		};

		var rootGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Padding = 20,
			RowSpacing = 12
		};
		rootGrid.Add(instructionLabel);
		rootGrid.Add(statusLayout, row: 1);
		rootGrid.Add(_fruitsCollectionView, row: 2);
		Content = rootGrid;
	}

#if WINDOWS
	void TryCaptureInitialMeasurement()
	{
		if (_initialMeasurementCaptured || !TryCreateMeasurement(0, out string measurement))
			return;

		_initialMeasurementCaptured = true;
		_initialMeasurementLabel.Text = measurement;
	}

	void CaptureMeasurement(Label destination, int sequence)
	{
		destination.Text = TryCreateMeasurement(sequence, out string measurement)
			? measurement
			: $"Identity=Missing;Width=0;Height=0;Selected=False;Radius=-1;Indicator=False;SelectedBackground=False;Index={_selectedIndex};Sequence={sequence};Theme=Unavailable";
	}

	bool TryCreateMeasurement(int sequence, out string measurement)
	{
		if (_fruitsCollectionView.Handler is not { PlatformView: WListView listView } ||
			listView.Items.Count == 0 ||
			listView.ContainerFromItem(listView.Items[0]) is not WListViewItem firstItem)
		{
			measurement = string.Empty;
			return false;
		}

		string identity = GetObservedIdentity(firstItem);
		double maximumRadius = GetMaximumVisibleRadius(firstItem);
		bool hasSelectionIndicator = HasVisibleSelectionIndicator(firstItem);
		bool hasSelectedBackground = HasVisibleSelectedBackground(firstItem);
		measurement = string.Create(
			CultureInfo.InvariantCulture,
			$"Identity={identity};Width={firstItem.ActualWidth:0.###};Height={firstItem.ActualHeight:0.###};Selected={firstItem.IsSelected};Radius={maximumRadius:0.###};Indicator={hasSelectionIndicator};SelectedBackground={hasSelectedBackground};Index={_selectedIndex};Sequence={sequence};Theme={listView.ActualTheme}");
		return true;
	}

	static bool HasVisibleSelectedBackground(WDependencyObject root)
	{
		if (root is WListViewItemPresenter presenter && IsVisibleBrush(presenter.SelectedBackground))
			return true;

		int childCount = WVisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < childCount; i++)
		{
			if (HasVisibleSelectedBackground(WVisualTreeHelper.GetChild(root, i)))
				return true;
		}

		return false;
	}

	static bool IsVisibleBrush(WBrush brush) =>
		brush is not null &&
		brush.Opacity > 0 &&
		(brush is not WSolidColorBrush solidColorBrush || solidColorBrush.Color.A > 0);

	static string GetObservedIdentity(WDependencyObject root)
	{
		if (root is WTextBlock { Text.Length: > 0 } textBlock)
			return textBlock.Text;

		if (root is WFrameworkElement { DataContext: string dataContext })
			return dataContext;

		int childCount = WVisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < childCount; i++)
		{
			string identity = GetObservedIdentity(WVisualTreeHelper.GetChild(root, i));
			if (identity.Length > 0)
				return identity;
		}

		return string.Empty;
	}

	static double GetMaximumVisibleRadius(WDependencyObject root)
	{
		double maximumRadius = 0;
		if (root is WFrameworkElement
			{
				Visibility: WVisibility.Visible,
				ActualWidth: > 0,
				ActualHeight: > 0,
				Opacity: > 0
			})
		{
			if (root is WListViewItem item)
				maximumRadius = GetMaximumRadius(item.CornerRadius);
			else if (root is WBorder border)
				maximumRadius = GetMaximumRadius(border.CornerRadius);
			else if (root is WRectangle rectangle)
				maximumRadius = Math.Max(rectangle.RadiusX, rectangle.RadiusY);
		}

		int childCount = WVisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < childCount; i++)
			maximumRadius = Math.Max(maximumRadius, GetMaximumVisibleRadius(WVisualTreeHelper.GetChild(root, i)));

		return maximumRadius;
	}

	static bool HasVisibleSelectionIndicator(WDependencyObject root)
	{
		if (root is WFrameworkElement
			{
				Visibility: WVisibility.Visible,
				ActualWidth: > 0,
				ActualHeight: > 0,
				Opacity: > 0,
				Name: string name
			} && name.Contains("SelectionIndicator", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		int childCount = WVisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < childCount; i++)
		{
			if (HasVisibleSelectionIndicator(WVisualTreeHelper.GetChild(root, i)))
				return true;
		}

		return false;
	}

	static double GetMaximumRadius(WCornerRadius radius) =>
		Math.Max(
			Math.Max(radius.TopLeft, radius.TopRight),
			Math.Max(radius.BottomRight, radius.BottomLeft));
#endif
}

