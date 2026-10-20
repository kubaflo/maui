using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Views;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using AColor = Android.Graphics.Color;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui
{
	public static partial class WindowExtensions
	{
		internal static void UpdateTitle(this Activity platformWindow, IWindow window)
		{
			if (string.IsNullOrEmpty(window.Title))
				platformWindow.Title = ApplicationModel.AppInfo.Current.Name;
			else
				platformWindow.Title = window.Title;
		}

		internal static DisplayOrientation GetOrientation(this IWindow? window)
		{
			if (window == null)
				return DeviceDisplay.Current.MainDisplayInfo.Orientation;

			return window.Handler?.MauiContext?.GetPlatformWindow()?.Resources?.Configuration?.Orientation switch
			{
				Orientation.Landscape => DisplayOrientation.Landscape,
				Orientation.Portrait => DisplayOrientation.Portrait,
				Orientation.Square => DisplayOrientation.Portrait,
				_ => DisplayOrientation.Unknown
			};
		}

		internal static void UpdateWindowSoftInputModeAdjust(this IWindow platformView, SoftInput inputMode)
		{
			var activity = platformView?.Handler?.PlatformView as Activity ??
							platformView?.Handler?.MauiContext?.GetPlatformWindow();

			activity?
				.Window?
				.SetSoftInputMode(inputMode);
		}

		//TODO : Make it public in NET 11.
		internal static void ConfigureTranslucentSystemBars(this Window? window, Activity activity)
		{
			if (window is null)
			{
				return;
			}

			var windowInsetsController = WindowCompat.GetInsetsController(window, window.DecorView);
			if (windowInsetsController is not null)
			{
				var configuration = activity.Resources?.Configuration;
				var isLightTheme = configuration is null ||
					(configuration.UiMode & UiMode.NightMask) != UiMode.NightYes;

				// Resolve the color that is actually drawn behind the status bar and choose the
				// icon/text appearance based on its luminance. Material 3 themes never tint the
				// system bars with colorPrimary - there it is a saturated brand accent color, and
				// the status bar sits directly on top of the window background instead. The legacy
				// theme does tint the status bar area with colorPrimary, so it stays the reference
				// there. If the theme color cannot be resolved, preserve the theme-based behavior.
				var statusBarBackdropAttribute = RuntimeFeature.IsMaterial3Enabled
					? global::Android.Resource.Attribute.ColorBackground
					: global::Android.Resource.Attribute.ColorPrimary;

				if (TryGetThemeColor(activity, statusBarBackdropAttribute, out var statusBarColor))
					windowInsetsController.AppearanceLightStatusBars = IsLightColor(statusBarColor);
				else
					windowInsetsController.AppearanceLightStatusBars = isLightTheme;

				windowInsetsController.AppearanceLightNavigationBars = isLightTheme;
			}
		}

		static bool TryGetThemeColor(Activity activity, int attribute, out AColor color)
		{
			color = default;

			if (activity.Theme is null)
				return false;

			using var ta = activity.Theme.ObtainStyledAttributes([attribute]);

			if (!ta.HasValue(0))
				return false;

			color = new AColor(ta.GetColor(0, 0));
			return true;
		}

		static bool IsLightColor(AColor color) =>
			ColorUtils.CalculateLuminance(color.ToArgb()) > 0.5;
	}
}