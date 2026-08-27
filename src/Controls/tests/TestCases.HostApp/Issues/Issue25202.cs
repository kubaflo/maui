#if ANDROID
using Android.Content.Res;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.AppBar;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Platform;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;
using Bitmap = Android.Graphics.Bitmap;
using Canvas = Android.Graphics.Canvas;
using ViewGroup = Android.Views.ViewGroup;
#endif
using Microsoft.Maui.Controls.Shapes;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 25202, "Routed Shell page loses the styled toolbar background", PlatformAffected.Android)]
public class Issue25202 : Shell
{
	static readonly Color StyledToolbarColor = Color.FromArgb("#184E77");
	static int s_routeInstance;

	public Issue25202()
	{
		if (Application.Current is not null)
			Application.Current.UserAppTheme = AppTheme.Light;

		FlyoutBehavior = FlyoutBehavior.Disabled;
		Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));

		Items.Add(new ShellContent
		{
			Title = "Settings",
			Route = "Settings",
			ContentTemplate = new DataTemplate(() => new SettingsPage(this))
		});
	}

	sealed class SettingsPage : ContentPage
	{
		public SettingsPage(Issue25202 issueShell)
		{
			Title = "Settings";
			Shell.SetBackgroundColor(this, StyledToolbarColor);
			Shell.SetForegroundColor(this, Colors.White);

			var titleView = new Border
			{
				AutomationId = "SettingsTitleView",
				Margin = new Thickness(12, 6),
				Padding = new Thickness(24, 10),
				BackgroundColor = StyledToolbarColor,
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 24 },
				Content = new Label
				{
					AutomationId = "SettingsTitle",
					Text = "Settings",
					FontAttributes = FontAttributes.Bold,
					FontSize = 24,
					TextColor = Colors.White,
					VerticalTextAlignment = TextAlignment.Center
				}
			};
			Shell.SetTitleView(this, titleView);

			var measurement = new Label
			{
				AutomationId = "InitialMeasurement",
				Text = "PENDING"
			};
			var measurementReady = new Label
			{
				AutomationId = "InitialMeasurementReady",
				Text = "INITIAL_PENDING"
			};
			var navigateButton = new Button
			{
				AutomationId = "NavigateToLogin",
				Text = "Navigate to login"
			};
			navigateButton.Clicked += async (sender, args) =>
				await Shell.Current.GoToAsync(nameof(LoginPage));

			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 20,
				Children =
				{
					new Label { Text = "Language", FontSize = 18 },
					new Label { Text = "English", FontSize = 24 },
					measurement,
					measurementReady,
					navigateButton
				}
			};

			Loaded += (sender, args) =>
				issueShell.MeasureToolbarWhenRendered(this, titleView, measurement, measurementReady, "initial", "settings", false);
		}
	}

	public sealed class LoginPage : ContentPage
	{
		public LoginPage()
		{
			Title = "Log in";

			var titleView = new Frame
			{
				AutomationId = "LoginTitleView",
				Margin = new Thickness(12, 6),
				Padding = new Thickness(24, 10),
				BackgroundColor = StyledToolbarColor,
				CornerRadius = 24,
				HasShadow = false,
				Content = new Label
				{
					AutomationId = "LoginTitle",
					Text = "Log in",
					FontAttributes = FontAttributes.Bold,
					FontSize = 24,
					TextColor = Colors.White,
					VerticalTextAlignment = TextAlignment.Center
				}
			};
			Shell.SetTitleView(this, titleView);

			var measurement = new Label
			{
				AutomationId = "RoutedMeasurement",
				Text = "PENDING"
			};
			var measurementReady = new Label
			{
				AutomationId = "RoutedMeasurementReady",
				Text = "ROUTED_PENDING"
			};

			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 20,
				Children =
				{
					new Entry
					{
						AutomationId = "UsernameEntry",
						Placeholder = "Username"
					},
					measurement,
					measurementReady
				}
			};

			var routeToken = Interlocked.Increment(ref s_routeInstance).ToString(System.Globalization.CultureInfo.InvariantCulture);
			Loaded += (sender, args) =>
			{
				if (Shell.Current is Issue25202 issueShell)
					issueShell.MeasureToolbarWhenRendered(this, titleView, measurement, measurementReady, "routed", routeToken, true);
			};
		}
	}

	void MeasureToolbarWhenRendered(
		ContentPage page,
		View titleView,
		Label measurement,
		Label measurementReady,
		string phase,
		string routeToken,
		bool expectsBackButton)
	{
#if ANDROID
		const int maximumFrameAttempts = 120;
		var attempts = 0;

		void MeasureNextFrame()
		{
			page.Dispatcher.Dispatch(() =>
			{
				attempts++;
				if (TryMeasureToolbar(page, titleView, phase, routeToken, expectsBackButton, out var result))
				{
					measurement.Text = result;
					measurementReady.Text = phase == "initial" ? "INITIAL_READY" : "ROUTED_READY";
				}
				else if (attempts < maximumFrameAttempts)
				{
					MeasureNextFrame();
				}
				else
				{
					measurement.Text = $"{phase}|error=native toolbar did not reach a measurable rendered state";
					measurementReady.Text = phase == "initial" ? "INITIAL_ERROR" : "ROUTED_ERROR";
				}
			});
		}

		MeasureNextFrame();
#endif
	}

#if ANDROID
	bool TryMeasureToolbar(
		ContentPage page,
		View titleView,
		string phase,
		string routeToken,
		bool expectsBackButton,
		out string result)
	{
		result = string.Empty;
		if (!ReferenceEquals(CurrentPage, page) ||
			Handler is not ShellRenderer ||
			page.Handler?.PlatformView is not AView pageView ||
			titleView.Handler?.PlatformView is not AView nativeTitleView ||
			Application.Current?.RequestedTheme != AppTheme.Light ||
			pageView.Resources?.Configuration?.Orientation != Orientation.Portrait)
		{
			return false;
		}

		var parent = pageView.Parent;
		while (parent is AView parentView && parentView is not CoordinatorLayout)
			parent = parentView.Parent;

		if (parent is not CoordinatorLayout coordinator)
			return false;

		var toolbar = Descendants<MaterialToolbar>(coordinator).FirstOrDefault();
		if (toolbar is null ||
			toolbar.Width <= 0 ||
			toolbar.Height <= 0 ||
			nativeTitleView.Width <= 0 ||
			nativeTitleView.Height <= 0 ||
			(toolbar.NavigationIcon is not null) != expectsBackButton)
		{
			return false;
		}

		var toolbarLocation = new int[2];
		var titleLocation = new int[2];
		toolbar.GetLocationInWindow(toolbarLocation);
		nativeTitleView.GetLocationInWindow(titleLocation);

		var titleLeft = titleLocation[0] - toolbarLocation[0];
		var titleTop = titleLocation[1] - toolbarLocation[1];
		var titleRight = titleLeft + nativeTitleView.Width;
		var titleBottom = titleTop + nativeTitleView.Height;
		if (titleLeft < 0 ||
			titleTop < 0 ||
			titleRight > toolbar.Width ||
			titleBottom > toolbar.Height ||
			string.IsNullOrEmpty(titleView.AutomationId))
		{
			return false;
		}

		var bitmapConfig = Bitmap.Config.Argb8888;
		if (bitmapConfig is null)
			return false;

		using var bitmap = Bitmap.CreateBitmap(toolbar.Width, toolbar.Height, bitmapConfig);
		if (bitmap is null)
			return false;

		using var canvas = new Canvas(bitmap);
		toolbar.Draw(canvas);

		var expected = StyledToolbarColor.ToPlatform();
		var matching = 0;
		var total = 0;
		var colors = new Dictionary<int, int>();
		for (var y = 0; y < toolbar.Height; y += 2)
		{
			for (var x = 0; x < toolbar.Width; x += 2)
			{
				var outsideTitle = x >= titleRight ||
					(x >= titleLeft && (y < titleTop || y >= titleBottom));
				if (!outsideTitle)
					continue;

				var pixel = bitmap.GetPixel(x, y);
				var color = new AColor(pixel);
				total++;
				if (Math.Abs(color.R - expected.R) <= 8 &&
					Math.Abs(color.G - expected.G) <= 8 &&
					Math.Abs(color.B - expected.B) <= 8)
				{
					matching++;
				}

				colors[pixel] = colors.TryGetValue(pixel, out var count) ? count + 1 : 1;
			}
		}

		if (total == 0 || colors.Count == 0)
			return false;

		var dominant = new AColor(colors.MaxBy(pair => pair.Value).Key);
		result = string.Join(
			"|",
			phase,
			$"token={routeToken}",
			$"title={titleView.AutomationId}",
			"theme=Light",
			"orientation=Portrait",
			$"expected=#{expected.R:X2}{expected.G:X2}{expected.B:X2}",
			$"observed=#{dominant.R:X2}{dominant.G:X2}{dominant.B:X2}",
			$"matching={matching}",
			$"total={total}",
			$"back={expectsBackButton.ToString().ToLowerInvariant()}");
		return true;
	}

	static IEnumerable<T> Descendants<T>(ViewGroup root) where T : AView
	{
		for (var index = 0; index < root.ChildCount; index++)
		{
			var child = root.GetChildAt(index);
			if (child is T match)
				yield return match;

			if (child is ViewGroup childGroup)
			{
				foreach (var descendant in Descendants<T>(childGroup))
					yield return descendant;
			}
		}
	}
#endif
}

