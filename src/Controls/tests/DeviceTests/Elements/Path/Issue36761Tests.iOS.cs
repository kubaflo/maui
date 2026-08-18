#if MACCATALYST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Path, "Issue36761")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36761 : ControlsHandlerTestBase
	{
		const string AnimationName = "Issue36761Pulse";

		[Fact]
		public async Task FullyTransparentAnimatedPathDoesNotRetainPixels()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Path, PathHandler>();
				});
			});

			var animatedPath = new Path
			{
				AutomationId = "AnimatedPath",
				Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString("M 0,0 L 120,0 L 120,120 L 0,120 Z"),
				Fill = Color.FromArgb("#FF6F73"),
				WidthRequest = 120,
				HeightRequest = 120,
				HorizontalOptions = LayoutOptions.Center
			};
			var startButton = new Button
			{
				AutomationId = "StartAnimation",
				Text = "Start pulse animation"
			};
			var statusLabel = new Label
			{
				AutomationId = "ResultStatus",
				HorizontalTextAlignment = TextAlignment.Center,
				Text = "NO BUG:"
			};
			var layout = new StackLayout
			{
				Padding = 24,
				Spacing = 18,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						HorizontalTextAlignment = TextAlignment.Center,
						Text = "The red Path will pulse three times."
					},
					animatedPath,
					startButton,
					statusLabel
				}
			};
			var page = new ContentPage { Content = layout };

			var fadingFrameSources = Enumerable.Range(0, 3)
				.Select(_ => new TaskCompletionSource<FadingFrameObservation>())
				.ToArray();
			var completionSource = new TaskCompletionSource<bool>();
			var clicked = false;
			var animationLeftBaseline = false;
			var observedCycle = -1;
			UIView expectedNativePath = null;
			UIWindow nativeWindow = null;
			CGRect originalPathRect = CGRect.Empty;

			startButton.Clicked += (_, _) =>
			{
				clicked = true;
				startButton.IsEnabled = false;
				statusLabel.Text = "NO BUG:";
				animatedPath.Scale = 1;
				animatedPath.Opacity = 1;

				var pulseAnimation = new Animation();
				for (var cycle = 0; cycle < 3; cycle++)
				{
					var cycleIndex = cycle;
					var start = cycle / 3d;
					var midpoint = start + (1d / 6d);
					var end = (cycle + 1) / 3d;

					pulseAnimation.Add(start, midpoint, new Animation(value =>
					{
						animatedPath.Scale = value;
						animationLeftBaseline |= value < 1;
					}, 1, 0.2));
					pulseAnimation.Add(start, midpoint, new Animation(value =>
					{
						animatedPath.Opacity = value;
						animationLeftBaseline |= value < 1;

						if (value <= 0.2 && observedCycle < cycleIndex)
						{
							observedCycle = cycleIndex;
							fadingFrameSources[cycleIndex].TrySetResult(
								ObserveFadingFrame(cycleIndex, animatedPath, expectedNativePath, nativeWindow, originalPathRect));
						}
					}, 1, 0));
					pulseAnimation.Add(midpoint, end, new Animation(value => animatedPath.Scale = value, 0.2, 1));
					pulseAnimation.Add(midpoint, end, new Animation(value => animatedPath.Opacity = value, 0, 1));
				}

				pulseAnimation.Commit(
					page,
					AnimationName,
					16,
					3600,
					Easing.Linear,
					(_, cancelled) =>
					{
						if (!cancelled)
							statusLabel.Text = "BUG REPRODUCED:";

						startButton.IsEnabled = true;
						completionSource.TrySetResult(cancelled);
					});
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				expectedNativePath = Assert.IsAssignableFrom<UIView>(animatedPath.Handler.PlatformView);
				nativeWindow = Assert.IsAssignableFrom<UIWindow>(expectedNativePath.Window);
				originalPathRect = expectedNativePath.ConvertRectToView(expectedNativePath.Bounds, nativeWindow);

				Assert.Same(animatedPath, animatedPath.Handler.VirtualView);
				Assert.Equal("AnimatedPath", animatedPath.AutomationId);
				Assert.InRange(expectedNativePath.Bounds.Width, 119, 121);
				Assert.InRange(expectedNativePath.Bounds.Height, 119, 121);
				Assert.True(nativeWindow.Bounds.Width >= 120 && nativeWindow.Bounds.Height >= 120);
				Assert.True(originalPathRect.X >= 0 && originalPathRect.Y >= 0 &&
					originalPathRect.Right <= nativeWindow.Bounds.Right &&
					originalPathRect.Bottom <= nativeWindow.Bounds.Bottom);

				using (var baselineImage = CaptureWindow(nativeWindow))
				{
					var baselinePixels = AnalyzeRedPixels(baselineImage, originalPathRect, originalPathRect, nativeWindow.Bounds);
					Assert.True(baselinePixels.Total > 0, "The attached Path did not render visible red baseline pixels.");
				}

				await PerformClick(startButton);
				Assert.True(clicked, "The native button click was not delivered.");
				await AssertEventually(
					() => animationLeftBaseline,
					timeout: 1000,
					interval: 16,
					message: "The pulse animation did not leave its baseline state.");

				var observations = new List<FadingFrameObservation>();
				for (var cycle = 0; cycle < 3; cycle++)
				{
					var observation = await fadingFrameSources[cycle].Task.WaitAsync(TimeSpan.FromSeconds(2));
					Assert.Equal(cycle, observation.Cycle);
					Assert.Equal(cycle, observedCycle);
					Assert.True(observation.SameNativeView, $"Cycle {cycle + 1} replaced the native Path view.");
					Assert.InRange(observation.NativeAlpha, 0.15, 0.201);
					Assert.InRange(observation.ManagedOpacity, 0.15, 0.2);
					Assert.InRange(observation.ManagedScale, 0.32, 0.36);
					Assert.InRange(Math.Abs(observation.CenterX - originalPathRect.GetMidX()), 0, 1);
					Assert.InRange(Math.Abs(observation.CenterY - originalPathRect.GetMidY()), 0, 1);
					observations.Add(observation);
				}

				var cancelled = await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.False(cancelled, "The pulse animation was cancelled before all three cycles completed.");
				Assert.Equal("BUG REPRODUCED:", statusLabel.Text);

				Assert.True(
					observations.All(observation => observation.OutsideRedPixels == 0),
					"Path retained red pixels outside its current animated bounds. Expected 0 outside red pixels in every cycle.");
			});
		}

		static Task PerformClick(Button button)
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var nativeButton = Assert.IsAssignableFrom<UIButton>(button.Handler.PlatformView);
				nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
			});
		}

		static FadingFrameObservation ObserveFadingFrame(
			int cycle,
			Path animatedPath,
			UIView expectedNativePath,
			UIWindow nativeWindow,
			CGRect originalPathRect)
		{
			var currentNativePath = Assert.IsAssignableFrom<UIView>(animatedPath.Handler.PlatformView);
			var currentRect = currentNativePath.ConvertRectToView(currentNativePath.Bounds, nativeWindow);

			using var image = CaptureWindow(nativeWindow);
			var pixels = AnalyzeRedPixels(image, originalPathRect, currentRect, nativeWindow.Bounds);

			return new FadingFrameObservation(
				cycle,
				ReferenceEquals(expectedNativePath, currentNativePath),
				currentNativePath.Alpha,
				animatedPath.Opacity,
				animatedPath.Scale,
				currentRect.GetMidX(),
				currentRect.GetMidY(),
				pixels.Outside,
				pixels.Right,
				pixels.Bottom,
				pixels.Coordinates);
		}

		static UIImage CaptureWindow(UIWindow window)
		{
			using var renderer = new UIGraphicsImageRenderer(
				window.Bounds.Size,
				new UIGraphicsImageRendererFormat
				{
					Opaque = false,
					Scale = window.Screen.Scale
				});

			return renderer.CreateImage(_ => window.DrawViewHierarchy(window.Bounds, afterScreenUpdates: true));
		}

		static PixelCounts AnalyzeRedPixels(UIImage image, CGRect region, CGRect currentPathRect, CGRect windowBounds)
		{
			var cgImage = image.CGImage;
			Assert.NotNull(cgImage);

			var width = (int)cgImage.Width;
			var height = (int)cgImage.Height;
			var bytesPerRow = 4 * width;
			var data = new byte[bytesPerRow * height];

			using (var colorSpace = CGColorSpace.CreateDeviceRGB())
			using (var context = new CGBitmapContext(
				data,
				width,
				height,
				8,
				bytesPerRow,
				colorSpace,
				CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast))
			{
				context.DrawImage(new CGRect(0, 0, width, height), cgImage);
			}

			var scaleX = width / windowBounds.Width;
			var scaleY = height / windowBounds.Height;
			var minX = Math.Max(0, (int)Math.Floor(region.Left * scaleX));
			var maxX = Math.Min(width, (int)Math.Ceiling(region.Right * scaleX));
			var minY = Math.Max(0, (int)Math.Floor((windowBounds.Height - region.Bottom) * scaleY));
			var maxY = Math.Min(height, (int)Math.Ceiling((windowBounds.Height - region.Top) * scaleY));
			var centerX = region.GetMidX();
			var centerY = region.GetMidY();
			var total = 0;
			var outside = 0;
			var right = 0;
			var bottom = 0;
			var coordinates = new List<string>();

			for (var y = minY; y < maxY; y++)
			{
				for (var x = minX; x < maxX; x++)
				{
					var offset = (bytesPerRow * y) + (4 * x);
					var red = data[offset];
					var green = data[offset + 1];
					var blue = data[offset + 2];
					var alpha = data[offset + 3];

					if (alpha <= 20 || red <= 40 || red <= green + 2 || red <= blue + 2)
						continue;

					total++;
					var pointX = (x + 0.5) / scaleX;
					var pointY = windowBounds.Height - ((y + 0.5) / scaleY);
					if (pointX >= currentPathRect.Left - 1 && pointX <= currentPathRect.Right + 1 &&
						pointY >= currentPathRect.Top - 1 && pointY <= currentPathRect.Bottom + 1)
						continue;

					outside++;
					var distanceX = pointX - centerX;
					var distanceY = pointY - centerY;
					if (distanceX >= Math.Abs(distanceY))
						right++;
					if (distanceY >= Math.Abs(distanceX))
						bottom++;
					if (coordinates.Count < 8)
						coordinates.Add($"({pointX:F1},{pointY:F1})");
				}
			}

			return new PixelCounts(total, outside, right, bottom, string.Join(",", coordinates));
		}

		sealed record FadingFrameObservation(
			int Cycle,
			bool SameNativeView,
			nfloat NativeAlpha,
			double ManagedOpacity,
			double ManagedScale,
			nfloat CenterX,
			nfloat CenterY,
			int OutsideRedPixels,
			int RightEdgePixels,
			int BottomEdgePixels,
			string Coordinates);

		sealed record PixelCounts(int Total, int Outside, int Right, int Bottom, string Coordinates);
	}
}
#endif
