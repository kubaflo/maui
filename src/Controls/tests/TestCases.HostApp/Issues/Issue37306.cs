#if ANDROID
using System.Globalization;
using AndroidX.Core.View;
using AView = Android.Views.View;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37306, "[Android] ScrollView clips content at the safe-area inset while scrolling", PlatformAffected.Android)]
public class Issue37306 : ContentPage
{
	public Issue37306()
	{
		SafeAreaEdges = SafeAreaEdges.None;

		var stack = new VerticalStackLayout
		{
			Padding = new Thickness(24, 8),
			Spacing = 12
		};

		var items = new List<Border>();
		for (var i = 0; i < 30; i++)
		{
			var item = new Border
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
			};

			items.Add(item);
			stack.Add(item);
		}

		var observedScrollY = -1d;
		var scrollView = new ScrollView
		{
			AutomationId = "Issue37306ScrollView",
			BackgroundColor = Color.FromArgb("#FFD54F"),
			Content = stack
		};

		void UpdateDiagnostics()
		{
			var insetBottom = GetBottomSystemBarInset();
			var scrolled = observedScrollY > 0;
			SemanticProperties.SetDescription(
				scrollView,
				$"Inset={insetBottom};Scroll={observedScrollY.ToString("0.##", CultureInfo.InvariantCulture)};Scrolled={scrolled.ToString().ToLowerInvariant()};Default={scrollView.SafeAreaEdges == SafeAreaEdges.Default}");

			for (var i = 0; i < items.Count; i++)
			{
				if (items[i].Handler?.PlatformView is not AView nativeItem)
					continue;

				var location = new int[2];
				nativeItem.GetLocationOnScreen(location);
				SemanticProperties.SetDescription(
					items[i],
					$"Item={i};Left={location[0]};Top={location[1]};Right={location[0] + nativeItem.Width};Bottom={location[1] + nativeItem.Height}");
			}
		}

		scrollView.Scrolled += (_, e) =>
		{
			observedScrollY = e.ScrollY;
			UpdateDiagnostics();
		};
		scrollView.Loaded += (_, _) => UpdateDiagnostics();
		SizeChanged += (_, _) => UpdateDiagnostics();

		Content = scrollView;
	}

	static int GetBottomSystemBarInset()
	{
		var decorView = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.DecorView;
		if (decorView is null)
			return -1;

		var windowInsets = ViewCompat.GetRootWindowInsets(decorView);
		return windowInsets?.GetInsets(WindowInsetsCompat.Type.SystemBars()).Bottom ?? -1;
	}
}
#endif

