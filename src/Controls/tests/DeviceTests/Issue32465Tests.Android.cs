using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using ABitmap = Android.Graphics.Bitmap;
using AColor = Android.Graphics.Color;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue32465")]
	public class Issue32465 : ControlsHandlerTestBase
	{
		const float GridSpacing = 64;

		[Fact]
		public async Task GridStrokeThicknessIsConsistent()
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
					handlers.AddHandler<GraphicsView, GraphicsViewHandler>();
				});
			});

			await VerifyBandClassifier();

			bool clicked = false;
			int capturedGeneration = -1;
			var gridDrawable = new GridDrawable { StrokeSize = 1 };
			var graphicsView = new GraphicsView
			{
				BackgroundColor = Colors.White,
				IsVisible = false
			};
			var renderButton = new Button
			{
				Text = "Render grid"
			};
			renderButton.Clicked += (_, _) =>
			{
				clicked = true;
				graphicsView.Drawable = gridDrawable;
				graphicsView.IsVisible = true;
				graphicsView.Invalidate();
			};

			var layout = new Grid
			{
				Padding = new Thickness(16),
				RowSpacing = 10,
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto }
				}
			};
			var descriptionLabel = new Label
			{
				Text = "StrokeSize 1 grid lines should have consistent thickness",
				TextColor = Colors.Black
			};
			var contextLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = "Grid stroke comparison",
				TextColor = Colors.Black
			};

			layout.Add(descriptionLabel);
			layout.Add(renderButton);
			layout.Add(graphicsView);
			layout.Add(contextLabel);
			Grid.SetRow(descriptionLabel, 0);
			Grid.SetRow(renderButton, 1);
			Grid.SetRow(graphicsView, 2);
			Grid.SetRow(contextLabel, 3);

			var page = new ContentPage
			{
				BackgroundColor = Colors.White,
				Content = layout,
				Title = "GraphicsView grid"
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.False(graphicsView.IsVisible);
				Assert.Null(graphicsView.Drawable);
				Assert.Equal(-1, gridDrawable.Generation);

				var graphicsHandler = Assert.IsType<GraphicsViewHandler>(graphicsView.Handler);
				var platformGraphicsView = Assert.IsType<Microsoft.Maui.Platform.PlatformTouchGraphicsView>(graphicsHandler.PlatformView);
				Assert.NotNull(platformGraphicsView);

				var buttonHandler = Assert.IsType<ButtonHandler>(renderButton.Handler);
				Assert.True(buttonHandler.PlatformView.PerformClick());

				await AssertHelpers.AssertEventually(
					() =>
					{
						capturedGeneration = gridDrawable.Generation;
						return clicked && graphicsView.IsVisible && capturedGeneration >= 0 &&
							platformGraphicsView.Width > 0 && platformGraphicsView.Height > 0;
					},
					message: "Issue32465 button click did not produce an attached GraphicsView draw");

				Assert.True(clicked);
				Assert.True(graphicsView.IsVisible);
				Assert.Same(gridDrawable, graphicsView.Drawable);

				using var bitmap = await platformGraphicsView.ToBitmap(MauiContext);
				capturedGeneration = gridDrawable.Generation;
				Assert.True(capturedGeneration >= 0);
				Assert.True(gridDrawable.DirtyRect.Width >= GridSpacing * 3);
				Assert.True(gridDrawable.DirtyRect.Height >= GridSpacing * 3);

				var measurement = MeasureBands(bitmap, gridDrawable.DirtyRect, 1);
				double expectedHorizontalWidth = bitmap.Height / gridDrawable.DirtyRect.Height;
				double expectedVerticalWidth = bitmap.Width / gridDrawable.DirtyRect.Width;

				Assert.True(
					Math.Abs(measurement.VerticalMedian - expectedVerticalWidth) <= 0.35,
					$"Issue32465 vertical grid stroke thickness was inconsistent: measured [{string.Join(", ", measurement.VerticalWidths)}], median {measurement.VerticalMedian:F2}, expected {expectedVerticalWidth:F2}");
				Assert.True(
					Math.Abs(measurement.HorizontalMedian - expectedHorizontalWidth) <= 0.35,
					$"Issue32465 horizontal grid stroke thickness was inconsistent: measured [{string.Join(", ", measurement.HorizontalWidths)}], median {measurement.HorizontalMedian:F2}, expected {expectedHorizontalWidth:F2}");
			});
		}

		async Task VerifyBandClassifier()
		{
			const float referenceStrokeSize = 8;
			var referenceDrawable = new GridDrawable { StrokeSize = referenceStrokeSize };
			var referenceView = new GraphicsView
			{
				BackgroundColor = Colors.White,
				Drawable = referenceDrawable,
				HeightRequest = 256,
				WidthRequest = 256
			};
			var referencePage = new ContentPage
			{
				BackgroundColor = Colors.White,
				Content = referenceView
			};

			await CreateHandlerAndAddToWindow(referencePage, async () =>
			{
				var handler = Assert.IsType<GraphicsViewHandler>(referenceView.Handler);
				var platformView = Assert.IsType<Microsoft.Maui.Platform.PlatformTouchGraphicsView>(handler.PlatformView);
				using var bitmap = await platformView.ToBitmap(MauiContext);

				Assert.True(referenceDrawable.Generation >= 0);
				Assert.True(referenceDrawable.DirtyRect.Width >= GridSpacing * 3);
				Assert.True(referenceDrawable.DirtyRect.Height >= GridSpacing * 3);

				var measurement = MeasureBands(bitmap, referenceDrawable.DirtyRect, referenceStrokeSize);
				double expectedHorizontalWidth = referenceStrokeSize * bitmap.Height / referenceDrawable.DirtyRect.Height;
				double expectedVerticalWidth = referenceStrokeSize * bitmap.Width / referenceDrawable.DirtyRect.Width;

				Assert.True(
					Math.Abs(measurement.HorizontalMedian - expectedHorizontalWidth) <= 1.1,
					$"Issue32465 reference horizontal stroke classifier measured {measurement.HorizontalMedian:F2}, expected {expectedHorizontalWidth:F2}");
				Assert.True(
					Math.Abs(measurement.VerticalMedian - expectedVerticalWidth) <= 1.1,
					$"Issue32465 reference vertical stroke classifier measured {measurement.VerticalMedian:F2}, expected {expectedVerticalWidth:F2}");
			});
		}

		static BandMeasurement MeasureBands(ABitmap bitmap, RectF dirtyRect, float strokeSize)
		{
			double scaleX = bitmap.Width / dirtyRect.Width;
			double scaleY = bitmap.Height / dirtyRect.Height;
			var verticalLines = LogicalSamples(dirtyRect.Width, GridSpacing);
			var horizontalLines = LogicalSamples(dirtyRect.Height, GridSpacing);
			var sampleColumns = LogicalSamples(dirtyRect.Width, GridSpacing / 2);
			var sampleRows = LogicalSamples(dirtyRect.Height, GridSpacing / 2);
			var verticalWidths = new List<int>();
			var horizontalWidths = new List<int>();

			foreach (float x in verticalLines)
			{
				foreach (float y in sampleRows)
					verticalWidths.Add(MeasureVerticalLine(bitmap, x * scaleX, y * scaleY, strokeSize * scaleX));
			}

			foreach (float y in horizontalLines)
			{
				foreach (float x in sampleColumns)
					horizontalWidths.Add(MeasureHorizontalLine(bitmap, x * scaleX, y * scaleY, strokeSize * scaleY));
			}

			Assert.NotEmpty(verticalWidths);
			Assert.NotEmpty(horizontalWidths);
			return new BandMeasurement(
				verticalWidths,
				horizontalWidths,
				Median(verticalWidths),
				Median(horizontalWidths));
		}

		static float[] LogicalSamples(float extent, float first)
		{
			var samples = new List<float>();
			for (float position = first; position < extent - 1 && samples.Count < 3; position += GridSpacing)
				samples.Add(position);

			Assert.Equal(3, samples.Count);
			return samples.ToArray();
		}

		static int MeasureVerticalLine(ABitmap bitmap, double expectedX, double sampleY, double expectedWidth)
		{
			int y = (int)Math.Round(sampleY);
			Assert.InRange(y, 0, bitmap.Height - 1);
			return MeasureBand(
				bitmap.Width,
				(int)Math.Round(expectedX),
				expectedWidth,
				x => IsGray(bitmap.GetPixel(x, y)));
		}

		static int MeasureHorizontalLine(ABitmap bitmap, double sampleX, double expectedY, double expectedWidth)
		{
			int x = (int)Math.Round(sampleX);
			Assert.InRange(x, 0, bitmap.Width - 1);
			return MeasureBand(
				bitmap.Height,
				(int)Math.Round(expectedY),
				expectedWidth,
				y => IsGray(bitmap.GetPixel(x, y)));
		}

		static int MeasureBand(int extent, int expectedCenter, double expectedWidth, Func<int, bool> isGray)
		{
			int searchRadius = (int)Math.Ceiling(expectedWidth) + 3;
			int searchStart = Math.Max(0, expectedCenter - searchRadius);
			int searchEnd = Math.Min(extent - 1, expectedCenter + searchRadius);
			int center = -1;

			for (int position = searchStart; position <= searchEnd; position++)
			{
				if (isGray(position))
				center = position;
			}

			Assert.True(center >= 0, $"Expected gray stroke near pixel {expectedCenter}");
			int start = center;
			int end = center;
			while (start > 0 && isGray(start - 1))
				start--;
			while (end < extent - 1 && isGray(end + 1))
				end++;

			Assert.True(start > 0 && end < extent - 1);
			Assert.False(isGray(start - 1));
			Assert.False(isGray(end + 1));
			return end - start + 1;
		}

		static bool IsGray(int pixel)
		{
			int red = AColor.GetRedComponent(pixel);
			int green = AColor.GetGreenComponent(pixel);
			int blue = AColor.GetBlueComponent(pixel);
			return red >= 80 && red <= 176 &&
				green >= 80 && green <= 176 &&
				blue >= 80 && blue <= 176;
		}

		static double Median(IReadOnlyList<int> values)
		{
			var sorted = values.OrderBy(value => value).ToArray();
			int middle = sorted.Length / 2;
			return sorted.Length % 2 == 0
				? (sorted[middle - 1] + sorted[middle]) / 2d
				: sorted[middle];
		}

		sealed class GridDrawable : IDrawable
		{
			public float StrokeSize { get; set; }

			public int Generation { get; private set; } = -1;

			public RectF DirtyRect { get; private set; }

			public void Draw(ICanvas canvas, RectF dirtyRect)
			{
				DirtyRect = dirtyRect;
				canvas.StrokeColor = Colors.Gray;
				canvas.StrokeSize = StrokeSize;

				for (float x = 0; x <= dirtyRect.Width; x += GridSpacing)
					canvas.DrawLine(x, 0, x, dirtyRect.Height);

				for (float y = 0; y <= dirtyRect.Height; y += GridSpacing)
					canvas.DrawLine(0, y, dirtyRect.Width, y);

				Generation++;
			}
		}

		sealed record BandMeasurement(
			IReadOnlyList<int> VerticalWidths,
			IReadOnlyList<int> HorizontalWidths,
			double VerticalMedian,
			double HorizontalMedian);
	}
}

