#if ANDROID
using System.Globalization;
using AView = Android.Views.View;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37306, "ScrollView content is clipped at the Android bottom safe-area inset", PlatformAffected.Android)]
public partial class Issue37306 : ContentPage
{
	int _scrollCallbackCount;

	public Issue37306()
	{
		InitializeComponent();

		Populate(IssueStack);
		Observe(IssueScrollView, IssueStack);
	}

	static void Populate(VerticalStackLayout stack)
	{
		for (var i = 0; i < 30; i++)
		{
			stack.Add(new Border
			{
				AutomationId = $"Issue37306Item{i}",
				StrokeThickness = 0,
				HeightRequest = 56,
				BackgroundColor = Colors.White,
				Content = new Label
				{
					Text = $"Item {i}",
					VerticalOptions = LayoutOptions.Center,
					HorizontalOptions = LayoutOptions.Center
				}
			});
		}
	}

	void Observe(ScrollView scrollView, VerticalStackLayout stack)
	{
		scrollView.Loaded += (_, _) => UpdateMeasurement(scrollView, stack, false);
		scrollView.Scrolled += (_, _) =>
		{
			_scrollCallbackCount++;
			UpdateMeasurement(scrollView, stack, true);
		};
	}

	void UpdateMeasurement(ScrollView scrollView, VerticalStackLayout stack, bool scrolled)
	{
		if (!ReferenceEquals(Content, scrollView))
			return;

#if ANDROID
		var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
		if (activity is null)
		{
			SemanticProperties.SetDescription(scrollView, "generation=reported;state=waiting-for-window");
			return;
		}

		var window = activity.Window;
		if (window is null)
		{
			SemanticProperties.SetDescription(scrollView, "generation=reported;state=waiting-for-window");
			return;
		}

		var decorView = window.DecorView;
		var rootInsets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(decorView);
		if (rootInsets is null)
		{
			SemanticProperties.SetDescription(scrollView, "generation=reported;state=waiting-for-insets");
			return;
		}

		var systemBars = rootInsets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
		var density = Microsoft.Maui.Devices.DeviceDisplay.Current.MainDisplayInfo.Density;
		var screenHeight = decorView.Height;
		var bandTop = screenHeight - systemBars.Bottom;
		var itemIndex = -1;
		var itemX = -1;
		var itemY = -1;
		var itemWidth = -1;
		var itemHeight = -1;
		var bestOverlap = 0;

		for (var i = 0; i < stack.Children.Count; i++)
		{
			if (stack.Children[i] is not Border border)
				continue;

			var itemHandler = border.Handler;
			if (itemHandler is null ||
				itemHandler.PlatformView is not AView nativeItem)
			{
				continue;
			}

			var location = new int[2];
			nativeItem.GetLocationOnScreen(location);
			var overlap = Math.Min(location[1] + nativeItem.Height, screenHeight) -
				Math.Max(location[1], bandTop);
			if (overlap > bestOverlap)
			{
				bestOverlap = overlap;
				itemIndex = i;
				itemX = location[0];
				itemY = location[1];
				itemWidth = nativeItem.Width;
				itemHeight = nativeItem.Height;
				break;
			}
		}

		var scrollX = -1;
		var scrollY = -1;
		var scrollWidth = -1;
		var scrollHeight = -1;
		var scrollPaddingBottom = -1;
		var scrollHandler = scrollView.Handler;
		if (scrollHandler is not null &&
			scrollHandler.PlatformView is AView nativeScrollView)
		{
			var scrollLocation = new int[2];
			nativeScrollView.GetLocationOnScreen(scrollLocation);
			scrollX = scrollLocation[0];
			scrollY = scrollLocation[1];
			scrollWidth = nativeScrollView.Width;
			scrollHeight = nativeScrollView.Height;
			scrollPaddingBottom = nativeScrollView.PaddingBottom;
		}

		var maximumOffset = Math.Max(0, stack.Height - scrollView.Height);
		SemanticProperties.SetDescription(scrollView, string.Create(
			CultureInfo.InvariantCulture,
			$"generation=reported;state={(scrolled ? "scrolled" : "ready")};edge={scrollView.SafeAreaEdges.Bottom};count={stack.Children.Count};callback={_scrollCallbackCount};offset={scrollView.ScrollY:F2};max={maximumOffset:F2};inset={systemBars.Bottom};density={density:F3};screen={screenHeight};item={itemIndex};ix={itemX};iy={itemY};iw={itemWidth};ih={itemHeight};sx={scrollX};sy={scrollY};sw={scrollWidth};sh={scrollHeight};spb={scrollPaddingBottom}"));
#else
		SemanticProperties.SetDescription(scrollView, "generation=reported;state=unsupported");
#endif
	}
}
