using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WLinearGradientBrush = Microsoft.UI.Xaml.Media.LinearGradientBrush;
using WStackPanel = Microsoft.UI.Xaml.Controls.StackPanel;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37149")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37149 : ControlsHandlerTestBase
	{
		const string ExpectedFailureSignature = "Issue 37149: Windows Shell TabBar background did not use the configured Shell.Background gradient.";

		[Fact]
		public async Task ShellBackgroundGradientAppliesToTabBar()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Border, BorderHandler>();
				});
			});

			var expectedStart = Color.FromArgb("#008000");
			var expectedEnd = Color.FromArgb("#00BFFF");
			var shellGradient = CreateGradient(expectedStart, expectedEnd);
			var referenceBorder = CreatePage("Home", expectedStart, expectedEnd, out var homePage);
			_ = CreatePage("Details", expectedStart, expectedEnd, out var detailsPage);

			var tabBar = new TabBar
			{
				Items =
				{
					new ShellContent { Title = "Home", Route = "Home", Content = homePage },
					new ShellContent { Title = "Details", Route = "Details", Content = detailsPage }
				}
			};

			var shell = new Shell
			{
				Background = shellGradient,
				Items = { tabBar }
			};

			var arrangedGradient = Assert.IsType<LinearGradientBrush>(shell.Background);
			Assert.Equal(new Point(0, 0), arrangedGradient.StartPoint);
			Assert.Equal(new Point(1, 0), arrangedGradient.EndPoint);
			Assert.Collection(
				arrangedGradient.GradientStops,
				stop =>
				{
					Assert.Equal(expectedStart, stop.Color);
					Assert.Equal(0f, stop.Offset);
				},
				stop =>
				{
					Assert.Equal(expectedEnd, stop.Color);
					Assert.Equal(1f, stop.Offset);
				});
			Assert.Null(Shell.GetTabBarBackgroundColor(homePage));

			MauiNavigationView observedNavigationView = null;
			WStackPanel observedTopNavArea = null;
			ContentPanel observedReferencePanel = null;
			WLinearGradientBrush observedReferenceBrush = null;

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Controls.Window(shell), async _ =>
			{
				await AssertEventually(() =>
				{
					observedNavigationView = shell.CurrentItem?.Handler?.PlatformView as MauiNavigationView;
					observedTopNavArea = observedNavigationView?.TopNavArea;
					observedReferencePanel = referenceBorder.Handler?.PlatformView as ContentPanel;
					observedReferenceBrush = observedReferencePanel?.BorderPath?.Fill as WLinearGradientBrush;

					return observedNavigationView is not null &&
						observedNavigationView.IsLoaded &&
						observedTopNavArea is not null &&
						observedTopNavArea.IsLoaded &&
						observedTopNavArea.ActualWidth > 0 &&
						observedTopNavArea.ActualHeight > 0 &&
						observedReferencePanel is not null &&
						observedReferencePanel.IsLoaded &&
						observedReferencePanel.ActualWidth > 0 &&
						observedReferencePanel.ActualHeight > 0 &&
						observedReferenceBrush is not null;
				}, timeout: 5000, message: "Shell TabBar and gradient reference did not finish loading.");

				Assert.NotNull(observedNavigationView);
				Assert.NotNull(observedTopNavArea);
				Assert.NotNull(observedReferencePanel);
				Assert.NotNull(observedReferenceBrush);
				AssertGradient(observedReferenceBrush, expectedStart, expectedEnd);

				var tabBarBrush = observedTopNavArea.Background as WLinearGradientBrush;
				var actualDescription = DescribeBrush(observedTopNavArea.Background, tabBarBrush);

				Assert.True(
					tabBarBrush is not null && GradientMatches(tabBarBrush, expectedStart, expectedEnd),
					$"{ExpectedFailureSignature} Actual brush: {actualDescription}.");
			});
		}

		static Border CreatePage(string title, Color start, Color end, out ContentPage page)
		{
			var referenceBorder = new Border
			{
				HeightRequest = 72,
				Stroke = Color.FromArgb("#404040"),
				StrokeThickness = 1,
				Background = CreateGradient(start, end),
				Content = new Label
				{
					Text = "Expected background",
					TextColor = Colors.White,
					FontAttributes = FontAttributes.Bold,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}
			};

			page = new ContentPage
			{
				Title = title,
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						Children =
						{
							new Label
							{
								Text = "Shell.Background gradient",
								FontSize = 24,
								FontAttributes = FontAttributes.Bold
							},
							new Label
							{
								Text = "The Windows navigation chrome and the TabBar should both use the same gradient shown below."
							},
							referenceBorder,
							new Label
							{
								Text = "Compare this reference with the Home and Details TabBar above. No explicit TabBar style or background is set."
							}
						}
					}
				}
			};

			return referenceBorder;
		}

		static LinearGradientBrush CreateGradient(Color start, Color end) =>
			new LinearGradientBrush(
				new GradientStopCollection
				{
					new GradientStop(start, 0),
					new GradientStop(end, 1)
				},
				new Point(0, 0),
				new Point(1, 0));

		static void AssertGradient(WLinearGradientBrush brush, Color expectedStart, Color expectedEnd)
		{
			Assert.Equal(2, brush.GradientStops.Count);
			Assert.Equal(0d, brush.GradientStops[0].Offset);
			Assert.Equal(1d, brush.GradientStops[1].Offset);
			Assert.Equal(expectedStart.ToWindowsColor(), brush.GradientStops[0].Color);
			Assert.Equal(expectedEnd.ToWindowsColor(), brush.GradientStops[1].Color);
			Assert.True(
				Math.Abs(brush.GradientStops[0].Color.B - brush.GradientStops[1].Color.B) > 20,
				"Gradient endpoints were not visually distinct.");
		}

		static bool GradientMatches(WLinearGradientBrush brush, Color expectedStart, Color expectedEnd) =>
			brush.GradientStops.Count == 2 &&
			brush.GradientStops[0].Offset == 0 &&
			brush.GradientStops[1].Offset == 1 &&
			brush.GradientStops[0].Color.Equals(expectedStart.ToWindowsColor()) &&
			brush.GradientStops[1].Color.Equals(expectedEnd.ToWindowsColor());

		static string DescribeBrush(object brush, WLinearGradientBrush gradient)
		{
			if (brush is null)
				return "null";

			if (gradient is null)
				return $"{brush.GetType().FullName}; stops=unavailable";

			if (gradient.GradientStops.Count == 0)
				return $"{brush.GetType().FullName}; stops=[]";

			if (gradient.GradientStops.Count == 1)
				return $"{brush.GetType().FullName}; stops=" +
					$"[{gradient.GradientStops[0].Color}@{gradient.GradientStops[0].Offset}]";

			return $"{brush.GetType().FullName}; stops=" +
				$"[{gradient.GradientStops[0].Color}@{gradient.GradientStops[0].Offset}, " +
				$"{gradient.GradientStops[1].Color}@{gradient.GradientStops[1].Offset}], " +
				$"count={gradient.GradientStops.Count}";
		}
	}
}

