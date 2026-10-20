using System;
using System.Threading.Tasks;
using Android.Content.Res;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Xunit;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37705")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37705 : ControlsHandlerTestBase
	{
		const string Material3Switch = "Microsoft.Maui.RuntimeFeature.IsMaterial3Enabled";

		[Fact]
		public async Task Material3LightStatusBarUsesDarkForeground()
		{
			AppContext.TryGetSwitch(Material3Switch, out bool originalMaterial3Value);

			try
			{
				AppContext.SetSwitch(Material3Switch, false);

				var activity = MauiProgram.CurrentContext.GetActivity();
				var platformWindow = activity.Window;
				var decorView = platformWindow.DecorView;
				var configuration = activity.Resources.Configuration;
				bool isLightTheme = (configuration.UiMode & UiMode.NightMask) != UiMode.NightYes;

				Assert.True(isLightTheme, "Issue37705 requires the stock light app theme.");

				var initialStatusBarController = WindowCompat.GetInsetsController(platformWindow, decorView);
				Assert.NotNull(initialStatusBarController);
				using (var initialAttributes =
					activity.Theme.ObtainStyledAttributes(new[] { global::Android.Resource.Attribute.ColorPrimary }))
				{
					Assert.True(initialAttributes.HasValue(0));
					var initialStatusBarColor = new AColor(initialAttributes.GetColor(0, 0));
					bool initialSurfaceIsLight =
						ColorUtils.CalculateLuminance(initialStatusBarColor.ToArgb()) > 0.5;
					Assert.Equal(initialSurfaceIsLight, initialStatusBarController.AppearanceLightStatusBars);
				}

				AppContext.SetSwitch(Material3Switch, true);
				Assert.True(RuntimeFeature.IsMaterial3Enabled);

				var titleLabel = new Label { Text = "Material 3 status bar contrast" };
				var descriptionLabel = new Label
				{
					Text = "The Android status bar above must show readable icons and text against its background."
				};
				var layout = new VerticalStackLayout
				{
					Children =
					{
						titleLabel,
						descriptionLabel
					}
				};
				var page = new ContentPage { Content = layout };
				var navigationPage = new NavigationPage(page);
				var window = new Window(navigationPage);
				bool loaded = false;
				var created = new TaskCompletionSource();
				var destroyed = new TaskCompletionSource();
				page.Loaded += (_, _) => loaded = true;
				window.Created += (_, _) => created.TrySetResult();
				window.Destroying += (_, _) => destroyed.TrySetResult();

				await InvokeOnMainThreadAsync(async () =>
				{
					var application = Application.Current;
					Assert.NotNull(application);

					application.OpenWindow(window);

					try
					{
						await created.Task.WaitAsync(TimeSpan.FromSeconds(5));
						await OnLoadedAsync(page);
						Assert.True(loaded);

						var windowHandler = Assert.IsType<WindowHandler>(window.Handler);
						Assert.NotNull(navigationPage.Handler);
						Assert.NotNull(page.Handler);
						Assert.NotNull(layout.Handler);
						Assert.NotNull(titleLabel.Handler);
						Assert.NotNull(descriptionLabel.Handler);

						_ = Assert.IsAssignableFrom<AView>(navigationPage.Handler.PlatformView);
						_ = Assert.IsAssignableFrom<AView>(page.Handler.PlatformView);
						_ = Assert.IsAssignableFrom<AView>(layout.Handler.PlatformView);
						_ = Assert.IsAssignableFrom<AView>(titleLabel.Handler.PlatformView);
						_ = Assert.IsAssignableFrom<AView>(descriptionLabel.Handler.PlatformView);

						var affectedActivity = windowHandler.PlatformView;
						var affectedPlatformWindow = affectedActivity.Window;
						var affectedDecorView = affectedPlatformWindow.DecorView;
						var statusBarController =
							WindowCompat.GetInsetsController(affectedPlatformWindow, affectedDecorView);
						Assert.NotNull(statusBarController);

						await AssertHelpers.AssertEventually(
							() =>
							{
								var insets = ViewCompat.GetRootWindowInsets(affectedDecorView);
								return insets is not null &&
									insets.GetInsets(WindowInsetsCompat.Type.StatusBars()).Top > 0;
							},
							timeout: 2000,
							message: "Issue37705 did not receive a nonzero runtime status-bar inset.");

						var rootInsets = ViewCompat.GetRootWindowInsets(affectedDecorView);
						Assert.NotNull(rootInsets);
						var statusBarInsets = rootInsets.GetInsets(WindowInsetsCompat.Type.StatusBars());
						Assert.True(statusBarInsets.Top > 0);

						var decorLocation = new int[2];
						affectedDecorView.GetLocationOnScreen(decorLocation);
						Assert.Equal(0, decorLocation[1]);
						Assert.True(affectedDecorView.Height >= statusBarInsets.Top);

						using var backgroundAttributes =
							affectedActivity.Theme.ObtainStyledAttributes(
								new[] { global::Android.Resource.Attribute.ColorBackground });
						Assert.True(backgroundAttributes.HasValue(0));
						var surfaceColor = new AColor(backgroundAttributes.GetColor(0, 0));
						double surfaceLuminance = ColorUtils.CalculateLuminance(surfaceColor.ToArgb());
						Assert.InRange(surfaceLuminance, 0.9, 1.0);

						Assert.True(
							statusBarController.AppearanceLightStatusBars,
							$"Issue37705 status bar contrast mismatch: surface luminance={surfaceLuminance:F3}, " +
							$"status-bar inset top={statusBarInsets.Top}, " +
							$"AppearanceLightStatusBars={statusBarController.AppearanceLightStatusBars}, expected=True.");
					}
					finally
					{
						application.CloseWindow(window);
						await destroyed.Task.WaitAsync(TimeSpan.FromSeconds(5));
					}
				});
			}
			finally
			{
				AppContext.SetSwitch(Material3Switch, originalMaterial3Value);
			}
		}
	}
}

