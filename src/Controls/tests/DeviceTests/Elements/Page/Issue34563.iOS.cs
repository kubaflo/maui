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
	public class Issue34563 : ControlsHandlerTestBase
	{
		const double PositionTolerance = 0.5;
		const string IndicatorText = "Affected child top edge";

		[Fact]
		public async Task ChildContainerSafeAreaRespectsTopWhenParentOnlyHandlesBottom()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var matchingEdges = new SafeAreaEdges(SafeAreaRegions.None);
			var matchingMeasurement = await AttachAndMeasureAsync(matchingEdges);

			Assert.True(
				Math.Abs(matchingMeasurement.ActualTop - matchingMeasurement.ExpectedTop) <= PositionTolerance,
				$"Issue34563 control child top was {matchingMeasurement.ActualTop:F1}, expected {matchingMeasurement.ExpectedTop:F1}, " +
				$"top inset {matchingMeasurement.TopInset:F1}, bottom inset {matchingMeasurement.BottomInset:F1}, tolerance {PositionTolerance:F1}");

			var mismatchedEdges = new SafeAreaEdges(
				SafeAreaRegions.None,
				SafeAreaRegions.None,
				SafeAreaRegions.None,
				SafeAreaRegions.Container);
			var affectedMeasurement = await AttachAndMeasureAsync(mismatchedEdges);

			Assert.True(
				Math.Abs(affectedMeasurement.ActualTop - affectedMeasurement.ExpectedTop) <= PositionTolerance,
				$"Issue34563 affected child top did not respect the iOS container safe area: actual {affectedMeasurement.ActualTop:F1}, " +
				$"expected {affectedMeasurement.ExpectedTop:F1}, top inset {affectedMeasurement.TopInset:F1}, " +
				$"bottom inset {affectedMeasurement.BottomInset:F1}, tolerance {PositionTolerance:F1}");
		}

		async Task<(double ActualTop, double ExpectedTop, double TopInset, double BottomInset)> AttachAndMeasureAsync(SafeAreaEdges pageEdges)
		{
			var topIndicator = new Label
			{
				Text = IndicatorText,
				BackgroundColor = Colors.Red,
				TextColor = Colors.White,
				FontAttributes = FontAttributes.Bold,
				FontSize = 18,
				HeightRequest = 48,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
			};

			var centerContent = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 14,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						Text = "iOS mismatched SafeAreaEdges",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold,
						HorizontalTextAlignment = TextAlignment.Center,
					},
					new Label
					{
						Text = "Page: Left=None, Top=None, Right=None, Bottom=Container",
						HorizontalTextAlignment = TextAlignment.Center,
					},
					new Label
					{
						Text = "Direct child Grid: Container on every edge",
						HorizontalTextAlignment = TextAlignment.Center,
					},
					new Label
					{
						Text = "The red child edge should remain below the status-bar safe area.",
						HorizontalTextAlignment = TextAlignment.Center,
					},
				},
			};

			var bottomContent = new VerticalStackLayout
			{
				Padding = new Thickness(24, 12),
				Spacing = 10,
				Children =
				{
					new Label
					{
						Text = "Measurement pending",
						HorizontalTextAlignment = TextAlignment.Center,
					},
					new Button
					{
						Text = "Check safe area placement",
					},
				},
			};

			var affectedGrid = new Grid
			{
				BackgroundColor = Colors.LightBlue,
				SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container),
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
				},
			};
			affectedGrid.Add(topIndicator, 0, 0);
			affectedGrid.Add(centerContent, 0, 1);
			affectedGrid.Add(bottomContent, 0, 2);

			var page = new ContentPage
			{
				BackgroundColor = Colors.LightBlue,
				SafeAreaEdges = pageEdges,
				Content = affectedGrid,
			};
			NavigationPage.SetHasNavigationBar(page, false);

			var loadedObservation = -1;
			var layoutObservation = -1;
			var loaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var laidOut = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			page.Loaded += OnLoaded;
			affectedGrid.SizeChanged += OnSizeChanged;

			var measurement = (
				ActualTop: double.NaN,
				ExpectedTop: double.NaN,
				TopInset: double.NaN,
				BottomInset: double.NaN);
			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await loaded.Task.WaitAsync(TimeSpan.FromSeconds(3));
				Assert.Equal(1, loadedObservation);

				await laidOut.Task.WaitAsync(TimeSpan.FromSeconds(3));
				Assert.Equal(1, layoutObservation);

				Assert.Equal(IndicatorText, topIndicator.Text);
				var nativeIndicator = topIndicator.Handler.PlatformView as UILabel;
				Assert.NotNull(nativeIndicator);
				Assert.Equal(IndicatorText, nativeIndicator.Text);

				await AssertEventually(
					() => nativeIndicator.Window is not null && Math.Abs(nativeIndicator.Bounds.Height - 48) <= PositionTolerance,
					timeout: 3000,
					message: "Issue34563 native indicator did not attach and reach its requested height");

				var nativeWindow = nativeIndicator.Window;
				Assert.NotNull(nativeWindow);
				Assert.True(nativeWindow.SafeAreaInsets.Top > 0, "Issue34563 requires a nonzero runtime iOS top safe-area inset");
				Assert.True(nativeWindow.SafeAreaInsets.Bottom > 0, "Issue34563 requires a nonzero runtime iOS bottom safe-area inset");
				Assert.Equal(48, nativeIndicator.Bounds.Height, PositionTolerance);

				var indicatorFrameInWindow = nativeIndicator.ConvertRectToView(nativeIndicator.Bounds, nativeWindow);
				measurement = (
					indicatorFrameInWindow.Y,
					nativeWindow.Bounds.Y + nativeWindow.SafeAreaInsets.Top,
					nativeWindow.SafeAreaInsets.Top,
					nativeWindow.SafeAreaInsets.Bottom);
			});

			return measurement;

			void OnLoaded(object sender, EventArgs e)
			{
				page.Loaded -= OnLoaded;
				loadedObservation = 1;
				loaded.TrySetResult(true);
			}

			void OnSizeChanged(object sender, EventArgs e)
			{
				if (affectedGrid.Width <= 0 || affectedGrid.Height <= 0)
					return;

				affectedGrid.SizeChanged -= OnSizeChanged;
				layoutObservation = 1;
				laidOut.TrySetResult(true);
			}
		}
	}
}
#endif

