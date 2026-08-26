#if ANDROID
using AndroidX.Core.View;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37306, "ScrollView clips content at the bottom safe-area inset while scrolling", PlatformAffected.Android)]
public class Issue37306 : ContentPage
{
	readonly ScrollView _testedScrollView;
	readonly Border _itemSevenBorder;

	public Issue37306()
	{
		SafeAreaEdges = SafeAreaEdges.None;

		var itemStack = new VerticalStackLayout
		{
			Padding = new Thickness(24, 8),
			Spacing = 12
		};

		_itemSevenBorder = CreateItem(7);
		for (var i = 0; i < 30; i++)
		{
			var item = i == 7 ? _itemSevenBorder : CreateItem(i);
			itemStack.Add(item);
		}

		_testedScrollView = new ScrollView
		{
			AutomationId = "Issue37306ScrollView",
			BackgroundColor = Color.FromArgb("#FFD54F"),
			Content = itemStack
		};
		_testedScrollView.Scrolled += (_, _) => UpdateDiagnostics("scrolled");
		_testedScrollView.SizeChanged += (_, _) => UpdateDiagnostics("ready");
		Loaded += (_, _) => UpdateDiagnostics("ready");

		Content = _testedScrollView;
	}

	static Border CreateItem(int index) =>
		new()
		{
			AutomationId = $"Issue37306Item{index}",
			StrokeThickness = 0,
			HeightRequest = 56,
			BackgroundColor = Colors.White,
			Content = new Label
			{
				Text = $"Item {index}",
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Center
			}
		};

	void UpdateDiagnostics(string state)
	{
		if (_testedScrollView.Handler?.PlatformView is not AViewGroup nativeScrollView ||
			_itemSevenBorder.Handler?.PlatformView is not AView nativeItem)
		{
			return;
		}

		var rootView = nativeScrollView.RootView;
		if (rootView is null)
			return;

		var windowInsets = ViewCompat.GetRootWindowInsets(rootView);
		if (windowInsets is null)
			return;

		var displayMetrics = nativeScrollView.Resources?.DisplayMetrics;
		if (displayMetrics is null)
			return;

		var frameLocation = new int[2];
		var itemLocation = new int[2];
		nativeScrollView.GetLocationOnScreen(frameLocation);
		nativeItem.GetLocationOnScreen(itemLocation);

		var systemBars = windowInsets.GetInsets(WindowInsetsCompat.Type.SystemBars());
		SemanticProperties.SetDescription(_testedScrollView,
			$"{state}|frame={frameLocation[0]},{frameLocation[1]},{nativeScrollView.Width},{nativeScrollView.Height}" +
			$"|density={displayMetrics.Density.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
			$"|inset={systemBars.Bottom}|offset={nativeScrollView.ScrollY}" +
			$"|paddingBottom={nativeScrollView.PaddingBottom}|clipToPadding={nativeScrollView.ClipToPadding}" +
			$"|item7={itemLocation[0]},{itemLocation[1]},{nativeItem.Width},{nativeItem.Height}");
	}
}
#endif

