using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

#if WINDOWS
namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37149")]
	public class Issue37149 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ShellBackgroundColorAppliesToUnstyledTabBar()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
				});
			});

			var expectedColor = Color.FromArgb("#663399");

			Shell CreateRecordedShell()
			{
				var firstContent = new ShellContent
				{
					Title = "First tab",
					ContentTemplate = new DataTemplate(() => new ContentPage
					{
						Title = "Shell background",
						Content = new VerticalStackLayout
						{
							Padding = 32,
							Spacing = 18,
							Children =
							{
								new Label
								{
									FontAttributes = FontAttributes.Bold,
									FontSize = 24,
									Text = "Shell.Background reference"
								},
								new Label
								{
									Text = "The purple swatch below is #663399, the same color assigned to Shell.Background. The unstyled tab strip should use this color too."
								},
								new BoxView
								{
									HeightRequest = 72,
									BackgroundColor = expectedColor
								}
							}
						}
					})
				};
				var secondContent = new ShellContent
				{
					Title = "Second tab",
					ContentTemplate = new DataTemplate(() => new ContentPage
					{
						Title = "Shell background",
						Content = new VerticalStackLayout
						{
							Padding = 32,
							Spacing = 18,
							Children =
							{
								new Label
								{
									FontAttributes = FontAttributes.Bold,
									FontSize = 24,
									Text = "Shell.Background reference"
								},
								new Label
								{
									Text = "The purple swatch below is #663399, the same color assigned to Shell.Background. The unstyled tab strip should use this color too."
								},
								new BoxView
								{
									HeightRequest = 72,
									BackgroundColor = expectedColor
								}
							}
						}
					})
				};
				var tabBar = new TabBar
				{
					Items = { firstContent, secondContent }
				};
				var shell = new Shell
				{
					Background = new SolidColorBrush(expectedColor),
					Items = { tabBar }
				};
				return shell;
			}

			var calibrationShell = CreateRecordedShell();
			Shell.SetTabBarBackgroundColor(calibrationShell, expectedColor);

			await CreateHandlerAndAddToWindow(calibrationShell, async () =>
			{
				var calibrationItemHandler = Assert.IsType<ShellItemHandler>(calibrationShell.CurrentItem.Handler);
				var calibrationNavigationView = Assert.IsType<MauiNavigationView>(calibrationItemHandler.PlatformView);
				var calibrationReady = false;

				await AssertEventually(() =>
				{
					var topNavArea = calibrationNavigationView.TopNavArea;
					calibrationReady = topNavArea is not null &&
						topNavArea.ActualWidth > 0 &&
						topNavArea.ActualHeight > 0;
					return calibrationReady;
				});

				Assert.True(calibrationReady);
				Assert.NotNull(calibrationNavigationView.TopNavArea);
				var calibrationBrush = Assert.IsType<WSolidColorBrush>(calibrationNavigationView.TopNavArea.Background);
				var calibrationNativeColor = calibrationBrush.Color;
				Assert.True(
					Math.Abs(calibrationNativeColor.A / 255d - expectedColor.Alpha) <= 1d / 255d &&
					Math.Abs(calibrationNativeColor.R / 255d - expectedColor.Red) <= 1d / 255d &&
					Math.Abs(calibrationNativeColor.G / 255d - expectedColor.Green) <= 1d / 255d &&
					Math.Abs(calibrationNativeColor.B / 255d - expectedColor.Blue) <= 1d / 255d,
					"Explicit Shell.TabBarBackgroundColor did not reach the native tab area.");
			});

			var firstTargetContent = new ShellContent
			{
				Title = "First tab",
				ContentTemplate = new DataTemplate(() => new ContentPage
				{
					Title = "Shell background",
					Content = new VerticalStackLayout
					{
						Padding = 32,
						Spacing = 18,
						Children =
						{
							new Label
							{
								FontAttributes = FontAttributes.Bold,
								FontSize = 24,
								Text = "Shell.Background reference"
							},
							new Label
							{
								Text = "The purple swatch below is #663399, the same color assigned to Shell.Background. The unstyled tab strip should use this color too."
							},
							new BoxView
							{
								HeightRequest = 72,
								BackgroundColor = expectedColor
							}
						}
					}
				})
			};
			var secondTargetContent = new ShellContent
			{
				Title = "Second tab",
				ContentTemplate = new DataTemplate(() => new ContentPage
				{
					Title = "Shell background",
					Content = new VerticalStackLayout
					{
						Padding = 32,
						Spacing = 18,
						Children =
						{
							new Label
							{
								FontAttributes = FontAttributes.Bold,
								FontSize = 24,
								Text = "Shell.Background reference"
							},
							new Label
							{
								Text = "The purple swatch below is #663399, the same color assigned to Shell.Background. The unstyled tab strip should use this color too."
							},
							new BoxView
							{
								HeightRequest = 72,
								BackgroundColor = expectedColor
							}
						}
					}
				})
			};
			var targetTabBar = new TabBar
			{
				Items = { firstTargetContent, secondTargetContent }
			};
			var targetShell = new Shell
			{
				Background = new SolidColorBrush(expectedColor),
				Items = { targetTabBar }
			};

			await CreateHandlerAndAddToWindow(targetShell, async () =>
			{
				Assert.IsType<ShellHandler>(targetShell.Handler);
				var targetItemHandler = Assert.IsType<ShellItemHandler>(targetShell.CurrentItem.Handler);
				var targetNavigationView = Assert.IsType<MauiNavigationView>(targetItemHandler.PlatformView);
				var nativeReady = false;
				byte observedAlpha = 0;
				byte observedRed = 0;
				byte observedGreen = 0;
				byte observedBlue = 0;

				await AssertEventually(() =>
				{
					var topNavArea = targetNavigationView.TopNavArea;
					if (topNavArea is null ||
						topNavArea.ActualWidth <= 0 ||
						topNavArea.ActualHeight <= 0)
					{
						return false;
					}

					nativeReady = true;
					return true;
				});

				Assert.True(nativeReady);
				Assert.Equal(2, targetTabBar.Items.Count);
				Assert.Same(firstTargetContent, targetTabBar.Items[0].CurrentItem);
				Assert.Same(secondTargetContent, targetTabBar.Items[1].CurrentItem);
				Assert.Same(firstTargetContent, targetShell.CurrentContent);
				Assert.Equal("First tab", targetShell.CurrentContent.Title);
				Assert.False(targetShell.IsSet(Shell.TabBarBackgroundColorProperty));
				Assert.Null(Shell.GetTabBarBackgroundColor(targetShell));
				Assert.True(targetShell.IsSet(VisualElement.BackgroundProperty));
				Assert.False(targetShell.IsSet(Shell.BackgroundColorProperty));
				var arrangedBrush = Assert.IsType<SolidColorBrush>(targetShell.Background);
				var arrangedColor = arrangedBrush.Color;
				Assert.Equal(expectedColor, arrangedColor);
				Assert.NotNull(targetNavigationView.TopNavArea);
				var targetBrush = targetNavigationView.TopNavArea.Background as WSolidColorBrush;
				Assert.True(
					targetBrush is not null,
					"Windows Shell tab bar background did not inherit Shell.Background. The loaded native tab area did not expose a solid background brush.");
				observedAlpha = targetBrush.Color.A;
				observedRed = targetBrush.Color.R;
				observedGreen = targetBrush.Color.G;
				observedBlue = targetBrush.Color.B;

				var tolerance = 1d / 255d;
				Assert.True(
					Math.Abs(observedAlpha / 255d - arrangedColor.Alpha) <= tolerance &&
					Math.Abs(observedRed / 255d - arrangedColor.Red) <= tolerance &&
					Math.Abs(observedGreen / 255d - arrangedColor.Green) <= tolerance &&
					Math.Abs(observedBlue / 255d - arrangedColor.Blue) <= tolerance,
					$"Windows Shell tab bar background did not inherit Shell.Background. Expected RGBA ({arrangedColor.Red:F4}, {arrangedColor.Green:F4}, {arrangedColor.Blue:F4}, {arrangedColor.Alpha:F4}); observed RGBA ({observedRed / 255d:F4}, {observedGreen / 255d:F4}, {observedBlue / 255d:F4}, {observedAlpha / 255d:F4}).");
			});
		}
	}
}
#endif

