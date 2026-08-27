#if WINDOWS
using System.Collections.Generic;
using WBorder = Microsoft.UI.Xaml.Controls.Border;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WControl = Microsoft.UI.Xaml.Controls.Control;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;
using WListViewItem = Microsoft.UI.Xaml.Controls.ListViewItem;
using WShape = Microsoft.UI.Xaml.Shapes.Shape;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WVisibility = Microsoft.UI.Xaml.Visibility;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35301, "Windows CollectionView applies WinUI styling on default", PlatformAffected.UWP)]
public class Issue35301 : ContentPage
{
	int _probeSequence = -1;

	public Issue35301()
	{
		var probeLabel = new Label
		{
			AutomationId = "NativeProbe",
			Text = "ProbeSequence=-1"
		};

		var collectionView = new CollectionView
		{
			SelectionMode = SelectionMode.Single,
			ItemsSource = new[] { "Apple", "Banana", "Cherry" },
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, ".");
				return label;
			})
		};

		var instructionLabel = new Label
		{
			Text = "Select Apple. The selected row should not gain platform-added shapes or indicators."
		};

		var grid = new Grid
		{
			Padding = 20,
			RowSpacing = 12,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};

		grid.Add(instructionLabel, 0, 0);
		grid.Add(probeLabel, 0, 1);
		grid.Add(collectionView, 0, 2);
		Content = grid;

		collectionView.Loaded += (_, _) => AttachProbe(collectionView, probeLabel);
	}

	void AttachProbe(CollectionView collectionView, Label probeLabel)
	{
		if (collectionView.Handler?.PlatformView is not WListViewBase listView)
			return;

		listView.LayoutUpdated += OnLayoutUpdated;
		listView.SelectionChanged += (_, _) =>
			listView.DispatcherQueue.TryEnqueue(() => UpdateProbe(collectionView, listView, probeLabel));

		void OnLayoutUpdated(object sender, object args)
		{
			if (GetAppleContainer(listView) is null)
				return;

			listView.LayoutUpdated -= OnLayoutUpdated;
			UpdateProbe(collectionView, listView, probeLabel);
		}
	}

	void UpdateProbe(CollectionView collectionView, WListViewBase listView, Label probeLabel)
	{
		var container = GetAppleContainer(listView);
		if (container is null)
			return;

		container.ApplyTemplate();
		var details = new List<string>();
		var visibleChromeCount = 0;

		foreach (var descendant in EnumerateVisualTree(container))
		{
			if (descendant is not WFrameworkElement element ||
				element.Visibility != WVisibility.Visible ||
				element.Opacity <= 0 ||
				element.ActualWidth <= 0 ||
				element.ActualHeight <= 0)
			{
				continue;
			}

			var nameIdentifiesSelectionChrome =
				element.Name.Contains("Select", StringComparison.OrdinalIgnoreCase);
			var brush = GetBackgroundBrush(descendant);
			var hasVisibleBrush = brush is not null && HasVisibleBrush(brush);
			var hasRoundedBackground = hasVisibleBrush && HasNonzeroCornerRadius(descendant);

			if (!nameIdentifiesSelectionChrome && !hasRoundedBackground)
				continue;

			visibleChromeCount++;
			details.Add(
				$"{element.GetType().Name}(Name={element.Name},Visibility={element.Visibility}," +
				$"Opacity={element.Opacity},Size={element.ActualWidth:F0}x{element.ActualHeight:F0}," +
				$"Brush={DescribeBrush(brush)},Corner={DescribeCornerRadius(descendant)})");
		}

		_probeSequence++;
		var managedSelection = collectionView.SelectedItem as string ?? "<null>";
		probeLabel.Text =
			$"ProbeSequence={_probeSequence}; ManagedSelected={managedSelection}; " +
			$"NativeSelected={container.IsSelected}; VisibleSelectionChrome={visibleChromeCount}; " +
			$"Details={(details.Count == 0 ? "None" : string.Join("|", details))}";
	}

	static WListViewItem GetAppleContainer(WListViewBase listView)
	{
		if (listView.Items.Count == 0)
			return null!;

		return listView.ContainerFromItem(listView.Items[0]) as WListViewItem ?? null!;
	}

	static IEnumerable<WDependencyObject> EnumerateVisualTree(WDependencyObject root)
	{
		yield return root;

		var childCount = WVisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < childCount; i++)
		{
			foreach (var descendant in EnumerateVisualTree(WVisualTreeHelper.GetChild(root, i)))
				yield return descendant;
		}
	}

	static WBrush GetBackgroundBrush(WDependencyObject element) =>
		element switch
		{
			WShape shape => shape.Fill,
			WBorder border => border.Background,
			WControl control => control.Background,
			_ => null!
		};

	static bool HasVisibleBrush(WBrush brush) =>
		brush is not WSolidColorBrush solidBrush || solidBrush.Color.A > 0;

	static bool HasNonzeroCornerRadius(WDependencyObject element) =>
		element switch
		{
			WBorder border => border.CornerRadius.TopLeft > 0 ||
				border.CornerRadius.TopRight > 0 ||
				border.CornerRadius.BottomRight > 0 ||
				border.CornerRadius.BottomLeft > 0,
			WControl control => control.CornerRadius.TopLeft > 0 ||
				control.CornerRadius.TopRight > 0 ||
				control.CornerRadius.BottomRight > 0 ||
				control.CornerRadius.BottomLeft > 0,
			_ => false
		};

	static string DescribeBrush(WBrush brush) =>
		brush switch
		{
			null => "null",
			WSolidColorBrush solidBrush => solidBrush.Color.ToString(),
			_ => brush.GetType().Name
		};

	static string DescribeCornerRadius(WDependencyObject element) =>
		element switch
		{
			WBorder border => border.CornerRadius.ToString(),
			WControl control => control.CornerRadius.ToString(),
			_ => "none"
		};
}
#endif

