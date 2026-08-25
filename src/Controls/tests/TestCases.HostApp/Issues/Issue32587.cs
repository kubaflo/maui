#if WINDOWS
using System.Globalization;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32587, "ContentView inside CollectionView reports invalid bounds during gesture events", PlatformAffected.UWP)]
public class Issue32587 : ContentPage
{
	public Issue32587()
	{
		var collectionView = new CollectionView
		{
			AutomationId = "Issue32587CollectionView",
			ItemsSource = new[] { "Gesture item" },
			ItemTemplate = new DataTemplate(() => new Issue32587GestureItemView())
		};

		var rootGrid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 16
		};

		rootGrid.Add(new Label
		{
			Text = "Tap the custom ContentView below to read its bounds from the gesture callback."
		});
		rootGrid.Add(collectionView, 0, 1);
		Content = rootGrid;
	}
}

public sealed class Issue32587GestureItemView : ContentView
{
	readonly VerticalStackLayout _itemLayout;
	readonly Label _geometryStateLabel;
	readonly Label _callbackSequenceLabel;
	readonly Label _captureStateLabel;
	int _tapCount;

	public Issue32587GestureItemView()
	{
		AutomationId = "Issue32587GestureItem";

		_geometryStateLabel = CreateProbeLabel("Issue32587GeometryState", "Geometry: pending");
		_callbackSequenceLabel = CreateProbeLabel("Issue32587CallbackSequence", "Callback sequence: -1");
		_captureStateLabel = CreateProbeLabel("Issue32587CaptureState", "Captured bounds: pending");

		_itemLayout = new VerticalStackLayout
		{
			BackgroundColor = Colors.LightBlue,
			Padding = 24,
			Spacing = 8,
			Children =
			{
				new Label
				{
					AutomationId = "Issue32587ItemIdentity",
					FontAttributes = FontAttributes.Bold,
					Text = "Gesture item"
				},
				_geometryStateLabel,
				_callbackSequenceLabel,
				_captureStateLabel
			}
		};

		Content = _itemLayout;
		_itemLayout.SizeChanged += (_, _) => UpdateGeometry();
		HandlerChanged += (_, _) =>
		{
			if (Handler?.PlatformView is WFrameworkElement platformView)
			{
				platformView.SizeChanged += (_, _) => UpdateGeometry();
				UpdateGeometry();
			}
		};
		Loaded += (_, _) => UpdateGeometry();

		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += (_, _) => CaptureBounds();
		GestureRecognizers.Add(tapGesture);
	}

	static Label CreateProbeLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text
		};

	void UpdateGeometry()
	{
		if (Handler?.PlatformView is not WFrameworkElement platformView)
			return;

		if (platformView.ActualWidth > 0 &&
			platformView.ActualHeight > 0 &&
			_itemLayout.Width > 0 &&
			_itemLayout.Height > 0)
		{
			_geometryStateLabel.Text = string.Create(
				CultureInfo.InvariantCulture,
				$"Geometry: NativeWidth={platformView.ActualWidth:R}; NativeHeight={platformView.ActualHeight:R}; ChildWidth={_itemLayout.Width:R}; ChildHeight={_itemLayout.Height:R}");
		}
	}

	void CaptureBounds()
	{
		_tapCount++;
		_callbackSequenceLabel.Text = $"Callback sequence: {_tapCount}";

		var nativeWidth = -1d;
		var nativeHeight = -1d;
		if (Handler?.PlatformView is WFrameworkElement platformView)
		{
			nativeWidth = platformView.ActualWidth;
			nativeHeight = platformView.ActualHeight;
		}

		_captureStateLabel.Text = string.Create(
			CultureInfo.InvariantCulture,
			$"Captured bounds: Width={Width:R}; Height={Height:R}; NativeWidth={nativeWidth:R}; NativeHeight={nativeHeight:R}");
	}
}
#endif

