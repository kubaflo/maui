#if MACCATALYST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using MauiContentView = Microsoft.Maui.Platform.ContentView;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Border)]
	[Category("Issue17525")]
	public class Issue17525 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task PolygonContentIsClippedToTrueInnerPath()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var polygon = new Polygon
			{
				Points = new PointCollection
				{
					new Point(40, 10),
					new Point(70, 80),
					new Point(10, 50),
				},
				StrokeThickness = 3,
			};

			var triangleContent = new Label
			{
				Text = "+",
				BackgroundColor = Color.FromArgb("#99FF0000"),
				TextColor = Color.FromArgb("#0088ee"),
				FontSize = 64,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
			};

			var triangleBorder = new Border
			{
				WidthRequest = 101,
				HeightRequest = 101,
				BackgroundColor = Colors.LightBlue,
				Stroke = Colors.LightGreen,
				StrokeThickness = 8,
				StrokeShape = polygon,
				Content = triangleContent,
			};

			var layoutObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			triangleBorder.SizeChanged += (_, _) =>
			{
				if (triangleBorder.Width > 0 && triangleBorder.Height > 0)
					layoutObserved.TrySetResult(true);
			};

			var grid = new Grid
			{
				ColumnSpacing = 10,
				RowSpacing = 10,
				VerticalOptions = LayoutOptions.Center,
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			grid.Add(triangleBorder, 0, 2);

			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 20,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						Text = "The red label should be clipped to the green triangle's inner edge.",
						HorizontalTextAlignment = TextAlignment.Center,
					},
					grid,
				},
			};

			var page = new ContentPage
			{
				Content = stack,
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await layoutObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
				await AssertEventually(() =>
					triangleBorder.Handler is BorderHandler borderHandler &&
					borderHandler.PlatformView.Subviews.SingleOrDefault()?.Layer.Mask is CAShapeLayer { Path: not null });

				Assert.Same(polygon, triangleBorder.StrokeShape);
				Assert.Equal(8, triangleBorder.StrokeThickness);
				Assert.Equal("+", triangleContent.Text);

				var handler = Assert.IsType<BorderHandler>(triangleBorder.Handler);
				var nativeBorder = Assert.IsType<MauiContentView>(handler.PlatformView);
				Assert.NotNull(triangleContent.Handler);
				Assert.NotNull(triangleContent.Handler.PlatformView);
				var nativeLabel = Assert.IsAssignableFrom<UILabel>(triangleContent.Handler.PlatformView);
				var maskedContent = Assert.Single(nativeBorder.Subviews);
				Assert.True(nativeLabel.IsDescendantOfView(maskedContent));
				Assert.InRange(nativeBorder.Bounds.Width, 100, 102);
				Assert.InRange(nativeBorder.Bounds.Height, 100, 102);

				var mask = Assert.IsAssignableFrom<CAShapeLayer>(maskedContent.Layer.Mask);
				Assert.NotNull(mask.Path);

				ValidateSquareOracle(nativeBorder.Bounds, maskedContent.Frame, mask, triangleBorder.StrokeThickness);

				var trianglePoints = new[]
				{
					new CGPoint(40, 10),
					new CGPoint(70, 80),
					new CGPoint(10, 50),
				};
				var strokeRegionSamples = CreateStrokeRegionSamples(trianglePoints, triangleBorder.StrokeThickness / 4)
					.Select(point => ConvertBorderPointToMaskPoint(point, maskedContent.Frame, mask))
					.ToList();

				foreach (var sample in strokeRegionSamples)
				{
					Assert.InRange(sample.X, mask.Bounds.Left, mask.Bounds.Right);
					Assert.InRange(sample.Y, mask.Bounds.Top, mask.Bounds.Bottom);
				}

				var nativeMaskPath = UIBezierPath.FromPath(mask.Path);
				int insideCount = strokeRegionSamples.Count(nativeMaskPath.ContainsPoint);

				Assert.True(
					insideCount == 0,
					$"Issue 17525 polygon content mask crossed the expected inner edge. Expected: 0 Actual: {insideCount}; Samples: {strokeRegionSamples.Count}; Frame: {nativeBorder.Frame}");
			});
		}

		static void ValidateSquareOracle(CGRect borderBounds, CGRect contentFrame, CAShapeLayer mask, double strokeThickness)
		{
			var square = new[]
			{
				new CGPoint(borderBounds.Left, borderBounds.Top),
				new CGPoint(borderBounds.Right, borderBounds.Top),
				new CGPoint(borderBounds.Right, borderBounds.Bottom),
				new CGPoint(borderBounds.Left, borderBounds.Bottom),
			};
			var innerTopLeft = ConvertBorderPointToMaskPoint(
				new CGPoint(borderBounds.Left + strokeThickness, borderBounds.Top + strokeThickness),
				contentFrame,
				mask);
			var innerBottomRight = ConvertBorderPointToMaskPoint(
				new CGPoint(borderBounds.Right - strokeThickness, borderBounds.Bottom - strokeThickness),
				contentFrame,
				mask);
			var expectedInnerPath = UIBezierPath.FromRect(new CGRect(
				innerTopLeft.X,
				innerTopLeft.Y,
				innerBottomRight.X - innerTopLeft.X,
				innerBottomRight.Y - innerTopLeft.Y));

			foreach (var point in CreateStrokeRegionSamples(square, strokeThickness / 2))
			{
				var sample = ConvertBorderPointToMaskPoint(point, contentFrame, mask);
				Assert.False(expectedInnerPath.ContainsPoint(sample));
			}
		}

		static CGPoint ConvertBorderPointToMaskPoint(CGPoint point, CGRect contentFrame, CAShapeLayer mask) =>
			new(
				point.X - contentFrame.X - mask.Position.X + (mask.AnchorPoint.X * mask.Bounds.Width) + mask.Bounds.X,
				point.Y - contentFrame.Y - mask.Position.Y + (mask.AnchorPoint.Y * mask.Bounds.Height) + mask.Bounds.Y);

		static List<CGPoint> CreateStrokeRegionSamples(IReadOnlyList<CGPoint> vertices, double distanceFromOuterEdge)
		{
			double signedArea = 0;
			for (int i = 0; i < vertices.Count; i++)
			{
				var current = vertices[i];
				var next = vertices[(i + 1) % vertices.Count];
				signedArea += (current.X * next.Y) - (next.X * current.Y);
			}

			var samples = new List<CGPoint>(vertices.Count * 3);
			double orientation = signedArea >= 0 ? 1 : -1;
			double[] fractions = [0.25, 0.5, 0.75];

			for (int i = 0; i < vertices.Count; i++)
			{
				var start = vertices[i];
				var end = vertices[(i + 1) % vertices.Count];
				double dx = end.X - start.X;
				double dy = end.Y - start.Y;
				double length = Math.Sqrt((dx * dx) + (dy * dy));
				double inwardX = orientation * -dy / length;
				double inwardY = orientation * dx / length;

				foreach (double fraction in fractions)
				{
					samples.Add(new CGPoint(
						start.X + (fraction * dx) + (distanceFromOuterEdge * inwardX),
						start.Y + (fraction * dy) + (distanceFromOuterEdge * inwardY)));
				}
			}

			return samples;
		}
	}
}
#endif

