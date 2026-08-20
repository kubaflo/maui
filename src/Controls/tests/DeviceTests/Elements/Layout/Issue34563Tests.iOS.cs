#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue34563")]
	[Category(TestCategory.Layout)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue34563 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ChildContainerTopRemainsBelowNativeSafeArea()
		{
			const double tolerance = 1;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var matchingEdges = new SafeAreaEdges(
				SafeAreaRegions.None,
				SafeAreaRegions.Container,
				SafeAreaRegions.None,
				SafeAreaRegions.Container);
			var reportedEdges = new SafeAreaEdges(
				SafeAreaRegions.None,
				SafeAreaRegions.None,
				SafeAreaRegions.None,
				SafeAreaRegions.Container);

			var referenceScene = CreatePage(matchingEdges);
			Assert.Equal(matchingEdges, referenceScene.Page.SafeAreaEdges);
			var reference = await MeasureTopIndicator(referenceScene);
			Assert.True(reference.IsApplicable, "Issue34563 requires a portrait iOS window with nonzero top and bottom safe-area insets.");

			Assert.True(
				Math.Abs(reference.ObservedY - reference.NativeTopInset) <= tolerance,
				$"Reference top indicator was at {reference.ObservedY:F1}, expected native safe-area top {reference.NativeTopInset:F1}.");

			var reportedScene = CreatePage(reportedEdges);
			Assert.Equal(reportedEdges, reportedScene.Page.SafeAreaEdges);
			var reported = await MeasureTopIndicator(reportedScene);
			Assert.True(reported.IsApplicable, "Issue34563 requires a portrait iOS window with nonzero top and bottom safe-area insets.");

			double expectedMinimum = reported.NativeTopInset - tolerance;
			Assert.True(
				reported.ObservedY >= expectedMinimum,
				$"Issue34563 top indicator must remain below the native top safe area: observedY={reported.ObservedY:F1}, nativeTopInset={reported.NativeTopInset:F1}, expectedMinimum={expectedMinimum:F1}, height={reported.Height:F1}, tolerance={tolerance:F1}.");

			async Task<(bool IsApplicable, double ObservedY, double NativeTopInset, double Height)> MeasureTopIndicator(
				(ContentPage Page, Grid ChildGrid, Label TopIndicator) scene)
			{
				bool loaded = false;
				double observedY = -1;
				scene.Page.Loaded += (_, _) => loaded = true;

				double nativeTopInset = 0;
				double nativeBottomInset = 0;
				double height = 0;
				bool isPortrait = false;

				await CreateHandlerAndAddToWindow<IWindowHandler>(scene.Page, async _ =>
				{
					Assert.True(loaded, "The ContentPage Loaded event must occur after root window attachment.");
					Assert.Equal(new SafeAreaEdges(SafeAreaRegions.Container), scene.ChildGrid.SafeAreaEdges);

					var nativeLabel = Assert.IsAssignableFrom<UILabel>(scene.TopIndicator.Handler.PlatformView);
					await AssertEventually(
						() => nativeLabel.Window is not null && nativeLabel.Bounds.Width > 0 && nativeLabel.Bounds.Height > 0,
						timeout: 5000,
						message: "The identified native top UILabel did not become visible in a UIWindow.");

					var nativeWindow = nativeLabel.Window;
					nativeTopInset = nativeWindow.SafeAreaInsets.Top;
					nativeBottomInset = nativeWindow.SafeAreaInsets.Bottom;
					isPortrait = nativeWindow.Bounds.Height > nativeWindow.Bounds.Width;
					height = nativeLabel.Bounds.Height;
					observedY = nativeLabel.ConvertRectToView(nativeLabel.Bounds, nativeWindow).Y;

					Assert.Same(scene.TopIndicator, ((LabelHandler)scene.TopIndicator.Handler).VirtualView);
					Assert.True(nativeLabel.Hidden is false && nativeLabel.Alpha > 0);
					Assert.True(observedY >= 0 && observedY + height <= nativeWindow.Bounds.Height);
					Assert.InRange(height, 52 - tolerance, 52 + tolerance);
				});

				return (
					isPortrait && nativeTopInset > 0 && nativeBottomInset > 0,
					observedY,
					nativeTopInset,
					height);
			}

			static (ContentPage Page, Grid ChildGrid, Label TopIndicator) CreatePage(SafeAreaEdges pageEdges)
			{
				var topIndicator = new Label
				{
					Text = "TOP CONTENT MUST STAY BELOW THE STATUS BAR",
					BackgroundColor = Color.FromArgb("#D93025"),
					TextColor = Colors.White,
					FontAttributes = FontAttributes.Bold,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
					HeightRequest = 52
				};

				var content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 18,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				content.Add(new Label
				{
					Text = "Page: top=None, bottom=Container",
					TextColor = Color.FromArgb("#202124"),
					HorizontalTextAlignment = TextAlignment.Center
				});
				content.Add(new Label
				{
					Text = "Child: all edges=Container",
					TextColor = Color.FromArgb("#202124"),
					HorizontalTextAlignment = TextAlignment.Center
				});
				content.Add(new Button { Text = "Check top safe area" });
				content.Add(new Label
				{
					Text = "NO BUG:",
					TextColor = Color.FromArgb("#137333"),
					FontAttributes = FontAttributes.Bold,
					FontSize = 20,
					HorizontalTextAlignment = TextAlignment.Center
				});

				var bottomIndicator = new Label
				{
					Text = "BOTTOM CONTENT",
					BackgroundColor = Color.FromArgb("#188038"),
					TextColor = Colors.White,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
					HeightRequest = 52
				};

				var childGrid = new Grid
				{
					SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container),
					BackgroundColor = Color.FromArgb("#F8F9FA"),
					RowDefinitions =
					{
						new RowDefinition { Height = GridLength.Auto },
						new RowDefinition { Height = GridLength.Star },
						new RowDefinition { Height = GridLength.Auto }
					}
				};
				childGrid.Add(topIndicator);
				childGrid.Add(content);
				childGrid.Add(bottomIndicator);
				Grid.SetRow(content, 1);
				Grid.SetRow(bottomIndicator, 2);

				var page = new ContentPage
				{
					SafeAreaEdges = pageEdges,
					BackgroundColor = Color.FromArgb("#202124"),
					Content = childGrid
				};
				NavigationPage.SetHasNavigationBar(page, false);

				return (page, childGrid, topIndicator);
			}
		}
	}
}
#endif

