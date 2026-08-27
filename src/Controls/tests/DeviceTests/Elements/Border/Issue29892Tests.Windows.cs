using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Xunit;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WCanvas = Microsoft.UI.Xaml.Controls.Canvas;
using WRectangle = Microsoft.UI.Xaml.Shapes.Rectangle;

namespace Microsoft.Maui.DeviceTests
{
	public class Issue29892 : ControlsHandlerTestBase
	{
		const double Tolerance = 1;

		[Fact]
		[Category("Issue29892")]
		public async Task ShadowVisualTracksBorderDuringRuntimeResize()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var titleLabel = new Label
			{
				Text = "Runtime Shadow Resize",
				FontSize = 28,
				FontAttributes = FontAttributes.Bold,
				HorizontalOptions = LayoutOptions.Center
			};
			var resizeButton = new Button
			{
				Text = "Resize shadowed view",
				HorizontalOptions = LayoutOptions.Center
			};
			var shadowedBorder = new Border
			{
				WidthRequest = 140,
				HeightRequest = 100,
				Stroke = Colors.Navy,
				StrokeThickness = 4,
				BackgroundColor = Colors.CornflowerBlue,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Shadow = new Microsoft.Maui.Controls.Shadow
				{
					Brush = Colors.Red,
					Offset = new Point(12, 12),
					Radius = 18,
					Opacity = 1
				},
				Content = new Label
				{
					Text = "Shadowed view",
					TextColor = Colors.White,
					FontAttributes = FontAttributes.Bold,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}
			};
			var statusLabel = new Label
			{
				Text = "Shadow size should follow the view",
				FontSize = 18,
				FontAttributes = FontAttributes.Bold,
				HorizontalOptions = LayoutOptions.Center
			};
			var grid = new Grid
			{
				Padding = 32,
				RowSpacing = 24,
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(titleLabel, 0, 0);
			grid.Add(resizeButton, 0, 1);
			grid.Add(shadowedBorder, 0, 2);
			grid.Add(statusLabel, 0, 3);

			int resizeCount = 0;
			resizeButton.Clicked += (_, _) =>
			{
				resizeCount++;
				bool useLargeSize = resizeCount % 2 == 1;
				shadowedBorder.WidthRequest = useLargeSize ? 320 : 140;
				shadowedBorder.HeightRequest = useLargeSize ? 220 : 100;
			};

			var page = new ContentPage
			{
				Title = "Runtime Shadow Resize",
				Content = grid
			};

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(page), async _ =>
			{
				var borderHandler = Assert.IsType<BorderHandler>(shadowedBorder.Handler);
				var platformBorder = Assert.IsType<ContentPanel>(borderHandler.PlatformView);
				var shadowWrapper = Assert.IsType<WrapperView>(borderHandler.ContainerView);
				var buttonHandler = Assert.IsType<ButtonHandler>(resizeButton.Handler);
				WButton platformButton = buttonHandler.PlatformView;
				var buttonPeer = new ButtonAutomationPeer(platformButton);
				var invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(buttonPeer.GetPattern(PatternInterface.Invoke));

				SpriteVisual shadowVisual = null;
				await AssertHelpers.AssertEventually(
					() =>
					{
						shadowVisual = GetShadowVisual(shadowWrapper);
						return shadowVisual != null &&
							IsClose(platformBorder.ActualWidth, 140) &&
							IsClose(platformBorder.ActualHeight, 100) &&
							IsClose(shadowVisual.Size.X, 140) &&
							IsClose(shadowVisual.Size.Y, 100);
					},
					timeout: 5000,
					message: "Initial Border and shadow visual did not reach 140x100.");

				Assert.NotNull(shadowVisual);
				Assert.True(IsClose(platformBorder.ActualWidth, 140) && IsClose(platformBorder.ActualHeight, 100),
					$"Initial native Border size was {platformBorder.ActualWidth}x{platformBorder.ActualHeight}, expected 140x100.");
				Assert.True(IsClose(shadowVisual.Size.X, 140) && IsClose(shadowVisual.Size.Y, 100),
					$"Initial shadow visual size was {shadowVisual.Size.X}x{shadowVisual.Size.Y}, expected 140x100.");

				double expectedWidth = -1;
				double expectedHeight = -1;
				int callbackCount = 0;
				TaskCompletionSource<ResizeSnapshot> resizeCompletion = null;

				platformBorder.SizeChanged += OnPlatformBorderSizeChanged;

				var growCycle1 = await InvokeResizeAndCapture(320, 220);
				var resetCycle = await InvokeResizeAndCapture(140, 100);
				var growCycle2 = await InvokeResizeAndCapture(320, 220);
				platformBorder.SizeChanged -= OnPlatformBorderSizeChanged;

				Assert.Equal(3, callbackCount);
				Assert.Equal(3, resizeCount);
				Assert.Same(borderHandler, shadowedBorder.Handler);
				Assert.Same(platformBorder, borderHandler.PlatformView);
				Assert.Same(shadowWrapper, borderHandler.ContainerView);
				Assert.Same(platformButton, buttonHandler.PlatformView);

				AssertResize(growCycle1, 320, 220, "grow cycle 1");
				AssertResize(resetCycle, 140, 100, "reset cycle");
				AssertResize(growCycle2, 320, 220, "grow cycle 2");

				async Task<ResizeSnapshot> InvokeResizeAndCapture(double width, double height)
				{
					expectedWidth = width;
					expectedHeight = height;
					resizeCompletion = new TaskCompletionSource<ResizeSnapshot>();

					invokeProvider.Invoke();

					return await resizeCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
				}

				void OnPlatformBorderSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs args)
				{
					if (!IsClose(args.NewSize.Width, expectedWidth) || !IsClose(args.NewSize.Height, expectedHeight))
						return;

					var currentShadowVisual = GetShadowVisual(shadowWrapper);
					double shadowWidth = currentShadowVisual == null ? -1 : currentShadowVisual.Size.X;
					double shadowHeight = currentShadowVisual == null ? -1 : currentShadowVisual.Size.Y;

					callbackCount++;
					resizeCompletion.TrySetResult(new ResizeSnapshot(
						callbackCount,
						platformBorder.ActualWidth,
						platformBorder.ActualHeight,
						shadowWidth,
						shadowHeight));
				}
			});
		}

		static SpriteVisual GetShadowVisual(WrapperView wrapper)
		{
			if (VisualTreeHelper.GetChildrenCount(wrapper) == 0 ||
				VisualTreeHelper.GetChild(wrapper, 0) is not WCanvas shadowCanvas ||
				VisualTreeHelper.GetChildrenCount(shadowCanvas) == 0 ||
				VisualTreeHelper.GetChild(shadowCanvas, 0) is not WRectangle shadowHost)
			{
				return null;
			}

			return ElementCompositionPreview.GetElementChildVisual(shadowHost) as SpriteVisual;
		}

		static bool IsClose(double actual, double expected) =>
			Math.Abs(actual - expected) <= Tolerance;

		static void AssertResize(ResizeSnapshot snapshot, double expectedWidth, double expectedHeight, string cycle)
		{
			Assert.True(
				IsClose(snapshot.NativeWidth, expectedWidth) && IsClose(snapshot.NativeHeight, expectedHeight),
				$"Native Border did not reach the requested size during {cycle}. Callback={snapshot.CallbackCount}; native Border={snapshot.NativeWidth}x{snapshot.NativeHeight}; shadow={snapshot.ShadowWidth}x{snapshot.ShadowHeight}; expected={expectedWidth}x{expectedHeight}; tolerance={Tolerance}.");
			Assert.True(
				IsClose(snapshot.ShadowWidth, expectedWidth) && IsClose(snapshot.ShadowHeight, expectedHeight),
				$"Shadow visual size did not match resized Border during {cycle}. Callback={snapshot.CallbackCount}; native Border={snapshot.NativeWidth}x{snapshot.NativeHeight}; shadow={snapshot.ShadowWidth}x{snapshot.ShadowHeight}; expected={expectedWidth}x{expectedHeight}; tolerance={Tolerance}.");
		}

		readonly record struct ResizeSnapshot(
			int CallbackCount,
			double NativeWidth,
			double NativeHeight,
			double ShadowWidth,
			double ShadowHeight);
	}
}

