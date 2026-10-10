using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using MauiShellHandler = Microsoft.Maui.Controls.Handlers.ShellHandler;
using MauiWindow = Microsoft.Maui.Controls.Window;
using WColor = Windows.UI.Color;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WNavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue34738")]
	public class Issue34738 : ControlsHandlerTestBase
	{
#if WINDOWS
		[Fact]
		public async Task DisabledTabUsesTabBarDisabledColor()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.TryAddHandler(typeof(MauiWindow), typeof(WindowHandler));
				});
			});

			var expectedColor = Colors.Green;
			var expectedNativeColor = expectedColor.ToWindowsColor();

			var oracleShell = await InvokeOnMainThreadAsync(() =>
			{
				var shell = CreateTwoTabShell(out _, out _);
				Shell.SetTabBarTitleColor(shell, expectedColor);
				return shell;
			});

			await CreateHandlerAndAddToWindow<MauiShellHandler>(oracleShell, async handler =>
			{
				var navigationView = oracleShell.CurrentItem.Handler.PlatformView as MauiNavigationView;
				Assert.NotNull(navigationView);

				var shellLoadedAndLaidOut = await Wait(
					() => handler.PlatformView.IsLoaded &&
						navigationView.IsLoaded &&
						navigationView.ActualWidth > 0 &&
						navigationView.ActualHeight > 0,
					timeout: 5000);
				Assert.True(shellLoadedAndLaidOut, "The title-color oracle Shell did not load and lay out.");

				WNavigationViewItem oracleItem = null;
				WTextBlock oracleText = null;
				WSolidColorBrush oracleBrush = null;
				var oracleBrushFound = await Wait(() =>
					TryFindTopTabTextBrush(
						navigationView,
						"Enabled Tab",
						out oracleItem,
						out oracleText,
						out oracleBrush),
					timeout: 5000);

				Assert.True(oracleBrushFound, "The enabled title-color oracle tab did not render a native text brush.");
				Assert.NotNull(oracleItem.Content);
				Assert.Equal("Enabled Tab", oracleItem.Content.ToString());
				Assert.True(IsDescendantOf(oracleText, navigationView.TopNavArea));
				Assert.Equal(expectedNativeColor, oracleBrush.Color);
			});

			TabBar reportedTabBar = null;
			Tab disabledTab = null;
			var reportedShell = await InvokeOnMainThreadAsync(() =>
			{
				var shell = CreateTwoTabShell(out reportedTabBar, out disabledTab);
				shell.FlyoutBehavior = FlyoutBehavior.Disabled;
				Shell.SetTabBarDisabledColor(shell, expectedColor);
				disabledTab.IsEnabled = false;
				return shell;
			});

			Assert.Equal(FlyoutBehavior.Disabled, reportedShell.FlyoutBehavior);
			Assert.Equal(expectedColor, Shell.GetTabBarDisabledColor(reportedShell));
			Assert.Equal(2, reportedTabBar.Items.Count);
			var enabledTab = reportedTabBar.Items[0];
			Assert.Equal("Enabled Tab", enabledTab.Title);
			Assert.NotNull(enabledTab.Icon);
			Assert.Single(enabledTab.Items);
			Assert.NotNull(enabledTab.Items[0].ContentTemplate);
			Assert.Same(disabledTab, reportedTabBar.Items[1]);
			Assert.Equal("Disabled Tab", disabledTab.Title);
			Assert.NotNull(disabledTab.Icon);
			Assert.Single(disabledTab.Items);
			Assert.NotNull(disabledTab.Items[0].ContentTemplate);
			Assert.False(disabledTab.IsEnabled);

			var observedArgb = -1L;
			await CreateHandlerAndAddToWindow<MauiShellHandler>(reportedShell, async handler =>
			{
				var navigationView = reportedShell.CurrentItem.Handler.PlatformView as MauiNavigationView;
				Assert.NotNull(navigationView);

				var shellLoadedAndLaidOut = await Wait(
					() => handler.PlatformView.IsLoaded &&
						navigationView.IsLoaded &&
						navigationView.ActualWidth > 0 &&
						navigationView.ActualHeight > 0,
					timeout: 5000);
				Assert.True(shellLoadedAndLaidOut, "The reported Shell did not load and lay out.");

				WNavigationViewItem nativeDisabledItem = null;
				WTextBlock nativeDisabledText = null;
				WSolidColorBrush nativeDisabledBrush = null;
				var disabledBrushFound = await Wait(() =>
				{
					if (!TryFindTopTabTextBrush(
						navigationView,
						"Disabled Tab",
						out nativeDisabledItem,
						out nativeDisabledText,
						out nativeDisabledBrush))
					{
						return false;
					}

					observedArgb = ToArgb(nativeDisabledBrush.Color);
					return true;
				}, timeout: 5000);

				Assert.True(disabledBrushFound, "The disabled tab did not render a native text brush.");
				Assert.NotEqual(-1L, observedArgb);
				Assert.NotNull(nativeDisabledItem.Content);
				Assert.Equal("Disabled Tab", nativeDisabledItem.Content.ToString());
				Assert.False(nativeDisabledItem.IsEnabled);
				Assert.True(IsDescendantOf(nativeDisabledText, navigationView.TopNavArea));
			});

			var expectedArgb = ToArgb(expectedNativeColor);
			Assert.True(
				observedArgb == expectedArgb,
				$"Disabled Shell tab text color was {FormatArgb(observedArgb)}; expected {FormatArgb(expectedArgb)} from TabBarDisabledColor Green.");
		}
#endif

		static Shell CreateTwoTabShell(out TabBar tabBar, out Tab disabledTab)
		{
			var enabledTab = new Tab
			{
				Title = "Enabled Tab",
				Icon = "groceries.png",
				Items =
				{
					new ShellContent
					{
						ContentTemplate = new DataTemplate(() => new ContentPage())
					}
				}
			};

			disabledTab = new Tab
			{
				Title = "Disabled Tab",
				Icon = "dotnet_bot.png",
				Items =
				{
					new ShellContent
					{
						ContentTemplate = new DataTemplate(() => new ContentPage())
					}
				}
			};

			tabBar = new TabBar
			{
				Items =
				{
					enabledTab,
					disabledTab
				}
			};

			return new Shell
			{
				Items =
				{
					tabBar
				}
			};
		}

		static bool TryFindTopTabTextBrush(
			MauiNavigationView navigationView,
			string title,
			out WNavigationViewItem navigationItem,
			out WTextBlock textBlock,
			out WSolidColorBrush brush)
		{
			navigationItem = null;
			textBlock = null;
			brush = null;

			var topNavArea = navigationView.TopNavArea;
			if (topNavArea is null)
				return false;

			navigationItem = FindNavigationItem(topNavArea, title);
			if (navigationItem is null)
				return false;

			textBlock = FindTextBlock(navigationItem, title);
			if (textBlock is null || textBlock.Foreground is not WSolidColorBrush textBrush)
				return false;

			brush = textBrush;
			return true;
		}

		static WNavigationViewItem FindNavigationItem(WDependencyObject parent, string title)
		{
			var childCount = WVisualTreeHelper.GetChildrenCount(parent);
			for (var index = 0; index < childCount; index++)
			{
				var child = WVisualTreeHelper.GetChild(parent, index);
				if (child is WNavigationViewItem item &&
					string.Equals(item.Content?.ToString(), title, StringComparison.Ordinal))
				{
					return item;
				}

				var descendant = FindNavigationItem(child, title);
				if (descendant is not null)
					return descendant;
			}

			return null;
		}

		static WTextBlock FindTextBlock(WDependencyObject parent, string title)
		{
			var childCount = WVisualTreeHelper.GetChildrenCount(parent);
			for (var index = 0; index < childCount; index++)
			{
				var child = WVisualTreeHelper.GetChild(parent, index);
				if (child is WTextBlock candidate &&
					string.Equals(candidate.Text, title, StringComparison.Ordinal))
				{
					return candidate;
				}

				var descendant = FindTextBlock(child, title);
				if (descendant is not null)
					return descendant;
			}

			return null;
		}

		static bool IsDescendantOf(WDependencyObject descendant, WDependencyObject ancestor)
		{
			var current = descendant;
			while (current is not null)
			{
				if (ReferenceEquals(current, ancestor))
					return true;

				current = WVisualTreeHelper.GetParent(current);
			}

			return false;
		}

		static long ToArgb(WColor color) =>
			((long)color.A << 24) |
			((long)color.R << 16) |
			((long)color.G << 8) |
			color.B;

		static string FormatArgb(long argb) => $"#{argb:X8}";
	}
}

