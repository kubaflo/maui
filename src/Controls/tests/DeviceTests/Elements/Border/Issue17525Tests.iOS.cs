#if MACCATALYST
using System;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue17525")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue17525 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task PolygonBorderClipsContentToCorrectlyInsetInnerPath()
		{
			const double borderSize = 101;
			const double strokeThickness = 8;
			const int colorTolerance = 35;

			var borderBackground = Colors.LightBlue;
			var contentBackground = Color.FromArgb("#99FF0000");

			Style CreateBorderStyle(IShape shape)
			{
				var style = new Style(typeof(Border));
				style.Setters.Add(new Setter { Property = Border.StrokeShapeProperty, Value = shape });
				style.Setters.Add(new Setter { Property = VisualElement.WidthRequestProperty, Value = borderSize });
				style.Setters.Add(new Setter { Property = VisualElement.HeightRequestProperty, Value = borderSize });
				style.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = borderBackground });
				style.Setters.Add(new Setter { Property = Border.StrokeThicknessProperty, Value = strokeThickness });
				style.Setters.Add(new Setter { Property = Border.StrokeProperty, Value = Colors.LightGreen });
				return style;
			}

			var contentStyle = new Style(typeof(Label));
			contentStyle.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = contentBackground });
			contentStyle.Setters.Add(new Setter { Property = Label.FontSizeProperty, Value = 64d });
			contentStyle.Setters.Add(new Setter { Property = Label.HorizontalTextAlignmentProperty, Value = TextAlignment.Center });
			contentStyle.Setters.Add(new Setter { Property = Label.VerticalTextAlignmentProperty, Value = TextAlignment.Center });
			contentStyle.Setters.Add(new Setter { Property = View.HorizontalOptionsProperty, Value = LayoutOptions.Center });
			contentStyle.Setters.Add(new Setter { Property = View.VerticalOptionsProperty, Value = LayoutOptions.Center });
			contentStyle.Setters.Add(new Setter { Property = Label.TextProperty, Value = "+" });
			contentStyle.Setters.Add(new Setter { Property = Label.TextColorProperty, Value = Color.FromArgb("#0088EE") });

			Label CreateContentLabel() => new Label { Style = contentStyle };

			var ellipseBorder = new Border
			{
				Style = CreateBorderStyle(new Ellipse()),
				Content = CreateContentLabel()
			};

			var roundedBorder = new Border
			{
				Style = CreateBorderStyle(new RoundRectangle()),
				Content = CreateContentLabel()
			};

			var polygon = new Polygon
			{
				Points = new PointCollection
				{
					new Point(40, 10),
					new Point(70, 80),
					new Point(10, 50)
				},
				StrokeThickness = 3
			};
			var polygonLabel = CreateContentLabel();
			var polygonBorder = new Border
			{
				Style = CreateBorderStyle(polygon),
				Content = polygonLabel
			};

			VerticalStackLayout CreateColumn(string caption, Border border) =>
				new VerticalStackLayout
				{
					Spacing = 6,
					Children =
					{
						new Label { Text = caption, HorizontalTextAlignment = TextAlignment.Center },
						border
					}
				};

			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				},
				ColumnSpacing = 18,
				HorizontalOptions = LayoutOptions.Center
			};
			grid.Add(CreateColumn("Ellipse", ellipseBorder), 0);
			grid.Add(CreateColumn("Rounded", roundedBorder), 1);
			grid.Add(CreateColumn("Triangle", polygonBorder), 2);

			var rootLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						Text = "Polygon Border inner-path clipping",
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						HorizontalTextAlignment = TextAlignment.Center
					},
					grid,
					new Label
					{
						Text = "Compare the red content clipping inside the green strokes.",
						HorizontalTextAlignment = TextAlignment.Center
					}
				}
			};
			var page = new ContentPage { Content = rootLayout };

			double loadedNativeFrameWidth = -1;
			void RecordNativeFrameWidth()
			{
				if (polygonBorder.Handler?.PlatformView is UIView platformView)
					loadedNativeFrameWidth = platformView.Frame.Width;
			}
			polygonBorder.Loaded += (_, _) => RecordNativeFrameWidth();
			polygonBorder.SizeChanged += (_, _) => RecordNativeFrameWidth();

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				Assert.True(loadedNativeFrameWidth > 0, "The Polygon Border did not complete native layout.");
				Assert.Same(polygonLabel, polygonBorder.Content);
				Assert.NotNull(ellipseBorder.Handler);
				Assert.NotNull(roundedBorder.Handler);
				Assert.NotNull(polygonBorder.Handler);
				Assert.NotNull(polygonLabel.Handler);

				var gridView = Assert.IsAssignableFrom<UIView>(grid.Handler.PlatformView);
				var roundedView = Assert.IsType<Microsoft.Maui.Platform.ContentView>(roundedBorder.Handler.PlatformView);
				var polygonView = Assert.IsType<Microsoft.Maui.Platform.ContentView>(polygonBorder.Handler.PlatformView);

				gridView.LayoutIfNeeded();
				roundedView.LayoutIfNeeded();
				polygonView.LayoutIfNeeded();

				Assert.Equal(borderSize, roundedView.Frame.Width, 1);
				Assert.Equal(borderSize, roundedView.Frame.Height, 1);
				Assert.Equal(borderSize, polygonView.Frame.Width, 1);
				Assert.Equal(borderSize, polygonView.Frame.Height, 1);

				var bitmapView = gridView.Superview is Microsoft.Maui.Platform.WrapperView wrapper ? wrapper : gridView;
				var roundedFrame = roundedView.ConvertRectToView(roundedView.Bounds, bitmapView);
				var polygonFrame = polygonView.ConvertRectToView(polygonView.Bounds, bitmapView);
				using var bitmap = await bitmapView.ToBitmap(MauiContext);
				var image = Assert.IsType<CGImage>(bitmap.CGImage);
				var pixelWidth = (int)image.Width;
				var pixelHeight = (int)image.Height;
				var pixels = new byte[pixelWidth * pixelHeight * 4];
				using var colorSpace = CGColorSpace.CreateDeviceRGB();
				using var bitmapContext = new CGBitmapContext(
					pixels,
					pixelWidth,
					pixelHeight,
					8,
					pixelWidth * 4,
					colorSpace,
					CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast);
				bitmapContext.DrawImage(new CGRect(0, 0, pixelWidth, pixelHeight), image);

				var pixelScale = pixelWidth / bitmapView.Bounds.Width;
				var expectedRed = (contentBackground.Alpha * contentBackground.Red) +
					((1 - contentBackground.Alpha) * borderBackground.Red);
				var expectedGreen = (contentBackground.Alpha * contentBackground.Green) +
					((1 - contentBackground.Alpha) * borderBackground.Green);
				var expectedBlue = (contentBackground.Alpha * contentBackground.Blue) +
					((1 - contentBackground.Alpha) * borderBackground.Blue);

				bool IsRedContentPixel(CGRect frame, double localX, double localY)
				{
					var x = (int)Math.Round((frame.X + localX) * pixelScale);
					var topY = (int)Math.Round((frame.Y + localY) * pixelScale);
					var y = pixelHeight - 1 - topY;
					if (x < 0 || x >= pixelWidth || y < 0 || y >= pixelHeight)
						return false;

					var offset = ((y * pixelWidth) + x) * 4;
					return Math.Abs(pixels[offset] - (expectedRed * 255)) <= colorTolerance &&
						Math.Abs(pixels[offset + 1] - (expectedGreen * 255)) <= colorTolerance &&
						Math.Abs(pixels[offset + 2] - (expectedBlue * 255)) <= colorTolerance;
				}

				var roundedInteriorRedCount = 0;
				for (var y = 0; y < borderSize; y++)
				{
					for (var x = 0; x < borderSize; x++)
					{
						if (!IsRedContentPixel(roundedFrame, x + 0.5, y + 0.5))
							continue;

						var edgeDistance = Math.Min(Math.Min(x + 0.5, y + 0.5),
							Math.Min(borderSize - x - 0.5, borderSize - y - 0.5));
						if (edgeDistance >= 16 && edgeDistance <= 35)
							roundedInteriorRedCount++;
					}
				}

				Assert.True(roundedInteriorRedCount > 0, "The RoundRectangle reference did not render red content pixels.");

				var outerPath = ((IShape)polygon).PathForBounds(
					new Microsoft.Maui.Graphics.Rect(0, 0, polygonView.Bounds.Width, polygonView.Bounds.Height));
				var outerPoints = outerPath.Points.Take(3).ToArray();
				Assert.Equal(3, outerPoints.Length);

				bool IsInsidePolygon(double x, double y)
				{
					var inside = false;
					for (var i = 0; i < outerPoints.Length; i++)
					{
						var previous = (i + outerPoints.Length - 1) % outerPoints.Length;
						var currentPoint = outerPoints[i];
						var previousPoint = outerPoints[previous];
						if ((currentPoint.Y > y) != (previousPoint.Y > y) &&
							x < ((previousPoint.X - currentPoint.X) * (y - currentPoint.Y) /
								(previousPoint.Y - currentPoint.Y)) + currentPoint.X)
							inside = !inside;
					}
					return inside;
				}

				double DistanceToNearestEdge(double x, double y)
				{
					var nearest = double.MaxValue;
					for (var i = 0; i < outerPoints.Length; i++)
					{
						var start = outerPoints[i];
						var end = outerPoints[(i + 1) % outerPoints.Length];
						var edgeX = end.X - start.X;
						var edgeY = end.Y - start.Y;
						var distance = Math.Abs((edgeX * (start.Y - y)) - ((start.X - x) * edgeY)) /
							Math.Sqrt((edgeX * edgeX) + (edgeY * edgeY));
						nearest = Math.Min(nearest, distance);
					}
					return nearest;
				}

				var polygonInteriorRedCount = 0;
				var leakCount = 0;
				for (var y = 0; y < borderSize; y++)
				{
					for (var x = 0; x < borderSize; x++)
					{
						var sampleX = x + 0.5;
						var sampleY = y + 0.5;
						if (!IsInsidePolygon(sampleX, sampleY))
							continue;

						var edgeDistance = DistanceToNearestEdge(sampleX, sampleY);
						if (!IsRedContentPixel(polygonFrame, sampleX, sampleY))
							continue;

						if (edgeDistance >= 12)
							polygonInteriorRedCount++;
						else if (edgeDistance >= strokeThickness - 2.5 &&
							edgeDistance <= strokeThickness - 0.5)
							leakCount++;
					}
				}

				Assert.True(polygonInteriorRedCount > 0, "The Polygon Border did not render identifiable red interior content.");
				Assert.True(leakCount == 0,
					$"Polygon Border content escaped the correctly inset inner path: observed {leakCount} red content pixels in the forbidden edge band; expected 0 with color tolerance {colorTolerance}.");
			});
		}
	}
}
#endif

