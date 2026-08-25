using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37149")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37149 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ShellBackgroundColorAppliesToDefaultTabBar()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers => SetupShellHandlers(handlers));
			});

			var shellBackgroundColor = Color.FromArgb("#FF1493");
			var explicitTabBarColor = Color.FromArgb("#1E90FF");
			var explicitShell = CreateShell(shellBackgroundColor);
			Shell.SetTabBarBackgroundColor(explicitShell, explicitTabBarColor);

			var explicitNativeBrush = await GetTabBarBackground(explicitShell, verifyItems: false);
			var explicitNativeBrushColor = Assert.IsType<WSolidColorBrush>(explicitNativeBrush).Color;
			Assert.Equal(explicitTabBarColor.ToWindowsColor(), explicitNativeBrushColor);

			var reportedShell = CreateShell(shellBackgroundColor);
			Assert.Equal(shellBackgroundColor, reportedShell.BackgroundColor);
			Assert.False(reportedShell.IsSet(Shell.TabBarBackgroundColorProperty));

			var reportedNativeBrush = await GetTabBarBackground(reportedShell, verifyItems: true);
			var expectedNativeColor = shellBackgroundColor.ToWindowsColor();
			var observedNativeColor = reportedNativeBrush is WSolidColorBrush solidColorBrush
				? solidColorBrush.Color.ToString()
				: reportedNativeBrush?.ToString() ?? "<null>";

			Assert.True(
				reportedNativeBrush is WSolidColorBrush reportedSolidColorBrush &&
					reportedSolidColorBrush.Color.Equals(expectedNativeColor),
				$"Windows Shell tab bar background did not inherit Shell.BackgroundColor: observed {observedNativeColor}, expected {expectedNativeColor}.");
		}

		static Shell CreateShell(Color backgroundColor)
		{
			var firstTab = new Tab
			{
				Title = "First tab",
				Items =
				{
					new ShellContent
					{
						Title = "Overview",
						Content = new ContentPage()
					}
				}
			};
			var secondTab = new Tab
			{
				Title = "Second tab",
				Items =
				{
					new ShellContent
					{
						Title = "Details",
						Content = new ContentPage()
					}
				}
			};

			return new Shell
			{
				BackgroundColor = backgroundColor,
				Items =
				{
					new TabBar
					{
						Items =
						{
							firstTab,
							secondTab
						}
					}
				}
			};
		}

		async Task<WBrush> GetTabBarBackground(Shell shell, bool verifyItems)
		{
			WBrush observedBrush = null;

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(shell, async _ =>
			{
				var shellItemHandler = shell.CurrentItem.Handler as ShellItemHandler;
				Assert.NotNull(shellItemHandler);

				var navigationView = shellItemHandler.PlatformView as MauiNavigationView;
				Assert.NotNull(navigationView);

				await AssertEventually(() => navigationView.TopNavArea is not null);
				var topNavigationArea = navigationView.TopNavArea;
				Assert.NotNull(topNavigationArea);

				if (verifyItems)
				{
					await AssertEventually(() => GetNavigationViewItems(navigationView).Count() == 2);
					var items = GetNavigationViewItems(navigationView).ToList();
					Assert.Equal(2, items.Count);
					Assert.Equal("First tab", items[0].Content);
					Assert.Equal("Second tab", items[1].Content);
				}

				observedBrush = topNavigationArea.Background;
			});

			return observedBrush;
		}
	}
}

