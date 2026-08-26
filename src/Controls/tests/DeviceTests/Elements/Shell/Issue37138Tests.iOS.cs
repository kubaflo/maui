#if IOS && !MACCATALYST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37138")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37138 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ShellBackgroundGradientRendersInNavigationAndTabBars()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Border, BorderHandler>();
				});
			});

			var referenceBorder = new Border
			{
				HeightRequest = 80,
				StrokeThickness = 0,
				Background = CreateIssue37138Brush(),
				Content = new Label
				{
					Text = "Red to blue reference",
					TextColor = Colors.White,
					FontAttributes = FontAttributes.Bold,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}
			};

			var firstPage = new ContentPage
			{
				Title = "Shell gradient",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 18,
					Children =
					{
						new Label
						{
							Text = "Expected Shell chrome gradient",
							FontAttributes = FontAttributes.Bold,
							FontSize = 20
						},
						referenceBorder,
						new Label
						{
							Text = "The same brush is assigned directly to Shell.Background. It should render in both the navigation bar above and the tab bar below.",
							FontSize = 16
						},
						new Button { Text = "Check Shell gradient" }
					}
				}
			};

			var tabBar = new TabBar
			{
				Items =
				{
					new ShellContent { Title = "First", Content = firstPage },
					new ShellContent
					{
						Title = "Second",
						Content = new ContentPage
						{
							Title = "Second",
							Content = new Label { Text = "Second page" }
						}
					}
				}
			};

			var shell = new Shell
			{
				Background = CreateIssue37138Brush(),
				Items = { tabBar }
			};
			var testWindow = new Window(shell);

			int loadedSentinel = -1;
			var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			firstPage.Loaded += (_, _) =>
			{
				loadedSentinel = 1;
				loaded.TrySetResult();
			};

			await CreateHandlerAndAddToWindow<ShellRenderer>(testWindow, async handler =>
			{
				await loaded.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.Equal(1, loadedSentinel);

				var shellItemRenderer = ((IShellContext)handler).CurrentShellItemRenderer as ShellItemRenderer;
				Assert.NotNull(shellItemRenderer);

				var navigationController = shellItemRenderer.SelectedViewController as UINavigationController;
				Assert.NotNull(navigationController);
				Assert.Same(shellItemRenderer, navigationController.ParentViewController);

				var navigationBar = navigationController.NavigationBar;
				var nativeTabBar = shellItemRenderer.TabBar;
				await AssertEventually(() =>
					navigationBar.Bounds.Width > 0 &&
					navigationBar.Bounds.Height > 0 &&
					nativeTabBar.Bounds.Width > 0 &&
					nativeTabBar.Bounds.Height > 0);

				var referenceView = (referenceBorder.Handler as IPlatformViewHandler)?.PlatformView as UIView;
				Assert.NotNull(referenceView);
				Assert.NotNull(referenceView.Window);
				Assert.Same(referenceView.Window, navigationBar.Window);
				Assert.Same(referenceView.Window, nativeTabBar.Window);

				var referenceImage = await referenceView.ToBitmap(MauiContext);
				var referenceObservation = ObserveIssue37138Gradient(referenceImage, 70);
				Assert.True(referenceObservation.Matches,
					$"Reference gradient pixel oracle was not established: {referenceObservation.Description}");

				var navigationImage = await navigationBar.ToBitmap(MauiContext);
				var tabImage = await nativeTabBar.ToBitmap(MauiContext);
				var navigationObservation = ObserveIssue37138Gradient(navigationImage, 180);
				var tabObservation = ObserveIssue37138Gradient(tabImage, 180);

				Assert.True(
					navigationObservation.Matches && tabObservation.Matches,
					$"Shell gradient rendering mismatch: navigation observed {navigationObservation.Description}; " +
					$"tab observed {tabObservation.Description}; expected horizontal red-to-blue gradient");
			});
		}

		static LinearGradientBrush CreateIssue37138Brush() =>
			new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 0),
				GradientStops =
				{
					new GradientStop(Colors.Red, 0),
					new GradientStop(Colors.Blue, 1)
				}
			};

		static Issue37138GradientObservation ObserveIssue37138Gradient(UIImage image, int tolerance)
		{
			double[] horizontalFractions = [0.08, 0.16, 0.84, 0.92];
			double[] verticalFractions = [0.18, 0.34];
			var samples = new List<(byte Red, byte Green, byte Blue)>();
			bool colorsMatch = true;

			foreach (double horizontalFraction in horizontalFractions)
			{
				foreach (double verticalFraction in verticalFractions)
				{
					var sample = SampleIssue37138Patch(image, horizontalFraction, verticalFraction);
					samples.Add(sample);
					var expected = (
						Red: (byte)Math.Round(255 * (1 - horizontalFraction)),
						Green: (byte)0,
						Blue: (byte)Math.Round(255 * horizontalFraction));
					colorsMatch &= Issue37138ColorDistance(sample, expected) <= tolerance;
				}
			}

			var left = samples.Take(4).ToArray();
			var right = samples.Skip(4).ToArray();
			var leftMedian = MedianIssue37138Color(left);
			var rightMedian = MedianIssue37138Color(right);
			int endpointSeparation = Issue37138ColorDistance(leftMedian, rightMedian);

			return new Issue37138GradientObservation(
				colorsMatch && endpointSeparation > 180,
				$"left rgb({leftMedian.Red},{leftMedian.Green},{leftMedian.Blue}), " +
				$"right rgb({rightMedian.Red},{rightMedian.Green},{rightMedian.Blue}), " +
				$"endpoint separation {endpointSeparation}");
		}

		static (byte Red, byte Green, byte Blue) SampleIssue37138Patch(
			UIImage image,
			double horizontalFraction,
			double verticalFraction)
		{
			int width = (int)image.CGImage.Width;
			int height = (int)image.CGImage.Height;
			Assert.True(width >= 10 && height >= 10, $"Rendered surface must be at least 10x10 pixels, but was {width}x{height}.");

			int centerX = Math.Clamp((int)Math.Round((width - 1) * horizontalFraction), 2, width - 3);
			int centerY = Math.Clamp((int)Math.Round((height - 1) * verticalFraction), 2, height - 3);
			var pixels = new List<(byte Red, byte Green, byte Blue)>(25);

			for (int y = centerY - 2; y <= centerY + 2; y++)
			{
				for (int x = centerX - 2; x <= centerX + 2; x++)
				{
					byte[] pixel = image.GetPixel(x, y);
					pixels.Add((pixel[0], pixel[1], pixel[2]));
				}
			}

			return MedianIssue37138Color(pixels);
		}

		static (byte Red, byte Green, byte Blue) MedianIssue37138Color(
			IEnumerable<(byte Red, byte Green, byte Blue)> pixels)
		{
			var pixelArray = pixels.ToArray();
			int middle = pixelArray.Length / 2;
			return (
				pixelArray.Select(pixel => pixel.Red).OrderBy(value => value).ElementAt(middle),
				pixelArray.Select(pixel => pixel.Green).OrderBy(value => value).ElementAt(middle),
				pixelArray.Select(pixel => pixel.Blue).OrderBy(value => value).ElementAt(middle));
		}

		static int Issue37138ColorDistance(
			(byte Red, byte Green, byte Blue) first,
			(byte Red, byte Green, byte Blue) second) =>
			Math.Abs(first.Red - second.Red) +
			Math.Abs(first.Green - second.Green) +
			Math.Abs(first.Blue - second.Blue);

		readonly record struct Issue37138GradientObservation(bool Matches, string Description);
	}
}
#endif

