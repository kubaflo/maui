using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WColor = Windows.UI.Color;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WNavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if WINDOWS
	[Category("Issue34738")]
	public class Issue34738 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DisabledTabTitleUsesTabBarDisabledColor()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers => SetupShellHandlers(handlers));
			});

			var configuredColor = Colors.Green;
			var calibrationLabel = new Label
			{
				Text = "Green calibration",
				TextColor = configuredColor
			};
			var enabledTab = new Tab
			{
				Title = "Enabled tab",
				Icon = "groceries.png",
				Items =
				{
					new ShellContent
					{
						Content = new ContentPage { Content = calibrationLabel }
					}
				}
			};
			var disabledTab = new Tab
			{
				Title = "Disabled tab",
				Icon = "dotnet_bot.png",
				IsEnabled = false,
				Items =
				{
					new ShellContent
					{
						Content = new ContentPage()
					}
				}
			};
			var shell = new Shell
			{
				Items =
				{
					new TabBar
					{
						Items = { enabledTab, disabledTab }
					}
				}
			};
			Shell.SetTabBarDisabledColor(shell, configuredColor);

			bool measurementRan = false;
			WColor? observedTitleColor = null;
			WColor? calibrationColor = null;
			ShellItemHandler shellItemHandler = null;
			MauiNavigationView navigationView = null;
			WNavigationViewItem disabledNativeItem = null;
			WTextBlock disabledTitle = null;
			WTextBlock calibrationText = null;

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(shell), async windowHandler =>
			{
				await AssertEventually(() =>
				{
					shellItemHandler = shell.CurrentItem?.Handler as ShellItemHandler;
					navigationView = shellItemHandler?.PlatformView as MauiNavigationView;
					disabledNativeItem = FindNavigationViewItem(navigationView, "Disabled tab");
					disabledTitle = FindTextBlock(disabledNativeItem, "Disabled tab");
					calibrationText = calibrationLabel.Handler?.PlatformView as WTextBlock;

					if (disabledTitle?.Foreground is not WSolidColorBrush titleBrush ||
						calibrationText?.Foreground is not WSolidColorBrush calibrationBrush ||
						disabledNativeItem.ActualWidth <= 0 ||
						disabledNativeItem.ActualHeight <= 0 ||
						disabledTitle.ActualWidth <= 0 ||
						disabledTitle.ActualHeight <= 0 ||
						calibrationText.ActualWidth <= 0 ||
						calibrationText.ActualHeight <= 0)
					{
						return false;
					}

					observedTitleColor = titleBrush.Color;
					calibrationColor = calibrationBrush.Color;
					measurementRan = true;
					return true;
				}, message: "Disabled tab native title was not available for color measurement");

				Assert.True(measurementRan);
				Assert.NotNull(windowHandler.PlatformView);
				Assert.NotNull(shell.Handler);
				Assert.NotNull(shell.CurrentItem);
				Assert.NotNull(shell.CurrentItem.Handler);
				Assert.NotNull(calibrationLabel.Handler);
				Assert.NotNull(shellItemHandler);
				Assert.NotNull(navigationView);
				Assert.NotNull(disabledNativeItem);
				Assert.NotNull(disabledTitle);
				Assert.NotNull(calibrationText);
				Assert.True(disabledNativeItem.ActualWidth > 0 && disabledNativeItem.ActualHeight > 0);
				Assert.True(disabledTitle.ActualWidth > 0 && disabledTitle.ActualHeight > 0);
				Assert.True(calibrationText.ActualWidth > 0 && calibrationText.ActualHeight > 0);
				Assert.Equal("Disabled tab", disabledTitle.Text);
				Assert.False(disabledNativeItem.IsEnabled);
				Assert.Equal(configuredColor, Shell.GetTabBarDisabledColor(shell));
				Assert.True(calibrationColor.HasValue);
				Assert.True(observedTitleColor.HasValue);

				var expectedNativeColor = configuredColor.ToWindowsColor();
				Assert.True(ColorsAreClose(calibrationColor.Value, expectedNativeColor),
					$"Calibration label native color did not match configured Green. Expected {Format(expectedNativeColor)}, actual {Format(calibrationColor.Value)}.");
				Assert.True(ColorsAreClose(observedTitleColor.Value, expectedNativeColor),
					"Disabled tab native title color did not match Shell.TabBarDisabledColor.");
			});
		}

		static WNavigationViewItem FindNavigationViewItem(WDependencyObject root, string title)
		{
			if (root is null)
				return null;

			if (root is WNavigationViewItem item && FindTextBlock(item, title) is not null)
				return item;

			var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
			for (var index = 0; index < childCount; index++)
			{
				var match = FindNavigationViewItem(
					Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, index),
					title);
				if (match is not null)
					return match;
			}

			return null;
		}

		static WTextBlock FindTextBlock(WDependencyObject root, string text)
		{
			if (root is null)
				return null;

			if (root is WTextBlock textBlock &&
				string.Equals(textBlock.Text, text, StringComparison.Ordinal))
			{
				return textBlock;
			}

			var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
			for (var index = 0; index < childCount; index++)
			{
				var match = FindTextBlock(
					Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, index),
					text);
				if (match is not null)
					return match;
			}

			return null;
		}

		static bool ColorsAreClose(WColor actual, WColor expected) =>
			Math.Abs(actual.A - expected.A) <= 1 &&
			Math.Abs(actual.R - expected.R) <= 1 &&
			Math.Abs(actual.G - expected.G) <= 1 &&
			Math.Abs(actual.B - expected.B) <= 1;

		static string Format(WColor color) =>
			$"RGBA({color.R}, {color.G}, {color.B}, {color.A})";
	}
#endif
}

