using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WLinearGradientBrush = Microsoft.UI.Xaml.Media.LinearGradientBrush;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37149")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37149 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ShellBackgroundGradientAppliesToTabBar()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.SetupShellHandlers();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandler>();
					handlers.AddHandler<Border, BorderHandler>();
				});
			});

			var shellBackground = CreateGradient();
			var firstTab = new Tab
			{
				Title = "First",
				Items =
				{
					new ShellContent
					{
						Title = "First",
						Content = CreatePage("First")
					}
				}
			};
			var secondTab = new Tab
			{
				Title = "Second",
				Items =
				{
					new ShellContent
					{
						Title = "Second",
						Content = CreatePage("Second")
					}
				}
			};
			var shell = new Shell
			{
				Background = shellBackground,
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

			bool shellLoaded = false;
			shell.Loaded += (_, _) => shellLoaded = true;

			var window = new Microsoft.Maui.Controls.Window(shell);
			await CreateHandlerAndAddToWindow<ShellHandler>(window, async handler =>
			{
				await AssertHelpers.AssertEventually(
					() => shellLoaded,
					message: "The Shell Loaded callback did not occur.");
				Assert.True(shellLoaded);

				await AssertHelpers.AssertEventually(
					() => handler.PlatformView.IsLoaded,
					message: "The native Shell view did not load.");
				Assert.True(handler.PlatformView.IsLoaded);

				await AssertHelpers.AssertEventually(
					() => shell.CurrentItem?.Handler?.PlatformView is MauiNavigationView,
					message: "The ShellItemHandler platform view was not created.");
				Assert.NotNull(shell.CurrentItem);
				Assert.NotNull(shell.CurrentItem.Handler);
				var navigationView = Assert.IsType<MauiNavigationView>(shell.CurrentItem.Handler.PlatformView);

				await AssertHelpers.AssertEventually(
					() => navigationView.IsLoaded,
					message: "The native tab navigation view did not load.");
				Assert.True(navigationView.IsLoaded);

				await AssertHelpers.AssertEventually(
					() => navigationView.TopNavArea is not null,
					message: "The native tab navigation template did not create TopNavArea.");
				Assert.NotNull(navigationView.TopNavArea);
				var topNavArea = navigationView.TopNavArea;
				await AssertHelpers.AssertEventually(
					() => topNavArea.IsLoaded,
					message: "The native TopNavArea did not load.");
				Assert.True(topNavArea.IsLoaded);

				await AssertHelpers.AssertEventually(
					() => navigationView.MenuItemsSource is IList<NavigationViewItemViewModel> items && items.Count == 2,
					message: "The native tab navigation view did not contain both tabs.");
				var menuItems = Assert.IsAssignableFrom<IList<NavigationViewItemViewModel>>(navigationView.MenuItemsSource);
				Assert.Collection(
					menuItems,
					item => Assert.Same(firstTab, item.Data),
					item => Assert.Same(secondTab, item.Data));

				var expectedStops = shellBackground.GradientStops;
				await AssertHelpers.AssertEventually(
					() => topNavArea.Background is not null,
					message: "The native TopNavArea background was not available.");
				var tabBarBackground = topNavArea.Background;
				Assert.True(
					MatchesGradient(tabBarBackground, expectedStops),
					$"Issue 37149: Windows Shell TabBar TopNavArea background was {DescribeBrush(tabBarBackground)}; expected LinearGradientBrush with stops {DescribeExpectedStops(expectedStops)}.");
			});
		}

		static ContentPage CreatePage(string title)
		{
			return new ContentPage
			{
				Title = title,
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							FontAttributes = FontAttributes.Bold,
							FontSize = 22,
							Text = "Shell.Background gradient"
						},
						new Label
						{
							Text = "The navigation bar and tab bar should both use the yellow-to-green gradient shown below."
						},
						new Border
						{
							HeightRequest = 72,
							Stroke = Colors.Black,
							StrokeThickness = 1,
							Background = CreateGradient(),
							Content = new Label
							{
								HorizontalOptions = LayoutOptions.Center,
								VerticalOptions = LayoutOptions.Center,
								Text = "Expected Shell background"
							}
						}
					}
				}
			};
		}

		static LinearGradientBrush CreateGradient()
		{
			return new LinearGradientBrush(
				new GradientStopCollection
				{
					new GradientStop(Colors.Yellow, 0),
					new GradientStop(Colors.Green, 1)
				},
				new Point(0, 0),
				new Point(1, 0));
		}

		static bool MatchesGradient(WBrush actualBrush, GradientStopCollection expectedStops)
		{
			if (actualBrush is not WLinearGradientBrush actualGradient ||
				actualGradient.GradientStops.Count != expectedStops.Count)
			{
				return false;
			}

			for (int i = 0; i < expectedStops.Count; i++)
			{
				if (actualGradient.GradientStops[i].Offset != expectedStops[i].Offset ||
					actualGradient.GradientStops[i].Color != expectedStops[i].Color.ToWindowsColor())
				{
					return false;
				}
			}

			return true;
		}

		static string DescribeBrush(WBrush brush)
		{
			if (brush is not WLinearGradientBrush gradient)
				return brush?.GetType().Name ?? "null";

			return $"LinearGradientBrush with stops [{DescribeStops(gradient)}]";
		}

		static string DescribeStops(WLinearGradientBrush gradient)
		{
			var stops = new string[gradient.GradientStops.Count];
			for (int i = 0; i < gradient.GradientStops.Count; i++)
			{
				var stop = gradient.GradientStops[i];
				stops[i] = $"{stop.Offset}:{stop.Color}";
			}

			return string.Join(", ", stops);
		}

		static string DescribeExpectedStops(GradientStopCollection expectedStops)
		{
			var stops = new string[expectedStops.Count];
			for (int i = 0; i < expectedStops.Count; i++)
				stops[i] = $"{expectedStops[i].Offset}:{expectedStops[i].Color.ToWindowsColor()}";

			return $"[{string.Join(", ", stops)}]";
		}
	}
}

