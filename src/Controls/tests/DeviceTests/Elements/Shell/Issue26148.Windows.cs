using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WAnimatedIcon = Microsoft.UI.Xaml.Controls.AnimatedIcon;
using WBitmapImage = Microsoft.UI.Xaml.Media.Imaging.BitmapImage;
using WColor = Windows.UI.Color;
using WImageIconSource = Microsoft.UI.Xaml.Controls.ImageIconSource;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue26148")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue26148 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ShellForegroundColorsCustomFlyoutIcon()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler(typeof(Microsoft.Maui.Controls.Window), typeof(WindowHandlerStub));
				});
			});

			var arrangedForeground = Colors.Red;
			var page = new ContentPage
			{
				Title = "Home",
				Content = new VerticalStackLayout
				{
					Children =
					{
						new Label { Text = "Issue 26148 FlyoutIcon color check" }
					}
				}
			};
			var shell = new Shell
			{
				FlyoutBehavior = FlyoutBehavior.Flyout,
				FlyoutIcon = "red.png",
				CurrentItem = new ShellContent { Title = "Home", Content = page }
			};
			Shell.SetForegroundColor(shell, arrangedForeground);

			WImageIconSource observedIconSource = null;

			await CreateHandlerAndAddToWindow<ShellHandler>(shell, async handler =>
			{
				Assert.NotNull(shell.Window);
				Assert.NotNull(handler.PlatformView);

				var expectedColor = Shell.GetForegroundColor(shell).ToWindowsColor();
				var colorOracle = new WImageIconSource
				{
					Foreground = new WSolidColorBrush(expectedColor)
				};

				Assert.True(TryReadForeground(colorOracle, out var oracleColor));
				Assert.True(ColorsMatch(oracleColor, expectedColor, 1));

				await AssertEventually(
					() => handler.PlatformView.ActualWidth > 0 && handler.PlatformView.ActualHeight > 0,
					5000);

				await AssertEventually(() =>
				{
					var togglePaneButton = handler.PlatformView.TogglePaneButton;
					if (togglePaneButton is null)
						return false;

					var animatedIcon = togglePaneButton.GetFirstDescendant<WAnimatedIcon>();
					if (animatedIcon?.FallbackIconSource is not WImageIconSource imageIconSource ||
						imageIconSource.ImageSource is not WBitmapImage bitmapImage ||
						bitmapImage.PixelWidth == 0)
					{
						return false;
					}

					observedIconSource = imageIconSource;
					return true;
				}, 5000);

				Assert.NotNull(observedIconSource);

				var hasObservedColor = TryReadForeground(observedIconSource, out var observedColor);
				Assert.True(
					hasObservedColor && ColorsMatch(observedColor, expectedColor, 1),
					$"Shell FlyoutIcon foreground mismatch: observed RGBA ({observedColor.R}, {observedColor.G}, {observedColor.B}, {observedColor.A}); expected RGBA ({expectedColor.R}, {expectedColor.G}, {expectedColor.B}, {expectedColor.A}).");
			});
		}

		static bool TryReadForeground(WImageIconSource iconSource, out WColor color)
		{
			if (iconSource.Foreground is WSolidColorBrush foreground)
			{
				color = foreground.Color;
				return true;
			}

			color = WColor.FromArgb(0, 0, 0, 0);
			return false;
		}

		static bool ColorsMatch(WColor actual, WColor expected, int tolerance) =>
			Math.Abs(actual.R - expected.R) <= tolerance &&
			Math.Abs(actual.G - expected.G) <= tolerance &&
			Math.Abs(actual.B - expected.B) <= tolerance &&
			Math.Abs(actual.A - expected.A) <= tolerance;

	}
}

