#if MACCATALYST
using System;
using System.Threading.Tasks;
using CoreAnimation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.ImageAnalysis;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Border)]
	[Category("Issue17525")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue17525 : ControlsHandlerTestBase
	{
		const double BorderSize = 101;
		const double BorderThickness = 8;
		const double EdgeTolerance = 1.5;

		[Fact]
		public async Task PolygonBorderClipsContentToInsetPath()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Slider, SliderHandler>();
				});
			});

			var resources = CreateResources();
			var circleLabelBorder = CreateBorder(resources, "BorderStyleCircle", CreateLabel(resources));
			var roundRectangleLabelBorder = CreateBorder(resources, "BorderStyleRoundRectangle", CreateLabel(resources));
			var polygonLabelBorder = CreateBorder(resources, "BorderStyleTriangle", CreateLabel(resources));
			var circleImage = CreateImage();
			var roundRectangleImage = CreateImage();
			var polygonImage = CreateImage();
			var circleImageBorder = CreateBorder(resources, "BorderStyleCircle", circleImage);
			var roundRectangleImageBorder = CreateBorder(resources, "BorderStyleRoundRectangle", roundRectangleImage);
			var polygonImageBorder = CreateBorder(resources, "BorderStyleTriangle", polygonImage);

			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				},
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star)
				},
				ColumnSpacing = 10,
				RowSpacing = 10,
				VerticalOptions = LayoutOptions.Center
			};

			AddToGrid(grid, circleLabelBorder, 0, 0);
			AddToGrid(grid, roundRectangleLabelBorder, 0, 1);
			AddToGrid(grid, polygonLabelBorder, 0, 2);
			AddToGrid(grid, circleImageBorder, 1, 0);
			AddToGrid(grid, roundRectangleImageBorder, 1, 1);
			AddToGrid(grid, polygonImageBorder, 1, 2);

			var content = new VerticalStackLayout
			{
				Spacing = 10,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					grid,
					new Label { Text = "Content Text" },
					new Entry { Text = "+" },
					new Label { Text = "Content Text FontSize" },
					new Slider { Minimum = 20, Maximum = 200, Value = 40 },
					new Label { Text = "Image Scale" },
					new Slider { Minimum = 1, Maximum = 20, Value = 1 }
				}
			};

			var page = new ContentPage
			{
				Title = "Border resize Content",
				Resources = resources,
				Content = content
			};

			double sizeChangedWidth = -1;
			polygonLabelBorder.SizeChanged += (_, _) =>
			{
				if (polygonLabelBorder.Width > 0 && polygonLabelBorder.Height > 0)
					sizeChangedWidth = polygonLabelBorder.Width;
			};

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(
				new Microsoft.Maui.Controls.Window(page),
				async _ =>
				{
					await AssertEventually(
						() => sizeChangedWidth > 0 && GetPlatformView(polygonLabelBorder).Frame.Width > 0,
						timeout: 5000,
						message: "Polygon Border did not complete its attached layout.");

					Assert.True(sizeChangedWidth > 0);
					Assert.Equal(6, grid.Children.Count);
					AssertGridPosition(circleLabelBorder, 0, 0);
					AssertGridPosition(roundRectangleLabelBorder, 0, 1);
					AssertGridPosition(polygonLabelBorder, 0, 2);
					AssertGridPosition(circleImageBorder, 1, 0);
					AssertGridPosition(roundRectangleImageBorder, 1, 1);
					AssertGridPosition(polygonImageBorder, 1, 2);

					AssertImageConfigured(circleImage);
					AssertImageConfigured(roundRectangleImage);
					AssertImageConfigured(polygonImage);

					var polygon = Assert.IsType<Polygon>(polygonLabelBorder.StrokeShape);
					Assert.Equal(3, polygon.Points.Count);
					Assert.Equal(new Point(40, 10), polygon.Points[0]);
					Assert.Equal(new Point(70, 80), polygon.Points[1]);
					Assert.Equal(new Point(10, 50), polygon.Points[2]);

					foreach (var border in new[]
					{
						circleLabelBorder,
						roundRectangleLabelBorder,
						polygonLabelBorder,
						circleImageBorder,
						roundRectangleImageBorder,
						polygonImageBorder
					})
					{
						var platformView = GetPlatformView(border);
						Assert.NotNull(platformView.Window);
						Assert.Equal(BorderSize, platformView.Frame.Width, 1);
						Assert.Equal(BorderSize, platformView.Frame.Height, 1);
						Assert.Equal(BorderThickness, border.StrokeThickness);
						Assert.Equal(Colors.LightBlue, border.BackgroundColor);
						Assert.Equal(Colors.LightGreen, ((SolidColorBrush)border.Stroke).Color);
					}

					var circleBitmap = await circleLabelBorder.AsRawBitmapAsync();
					var roundRectangleBitmap = await roundRectangleLabelBorder.AsRawBitmapAsync();
					var polygonBitmap = await polygonLabelBorder.AsRawBitmapAsync();

					Assert.True(CountGlyphPixels(circleBitmap) > 0);
					Assert.True(CountGlyphPixels(roundRectangleBitmap) > 0);
					Assert.True(CountGlyphPixels(polygonBitmap) > 0);

					var outerPolygon = new Point[polygon.Points.Count];
					for (int i = 0; i < polygon.Points.Count; i++)
						outerPolygon[i] = polygon.Points[i];

					var expectedInnerPolygon = InsetPolygon(outerPolygon, polygonLabelBorder.StrokeThickness);
					var expectedBounds = GetBounds(expectedInnerPolygon);
					var platformBorder = GetPlatformView(polygonLabelBorder);
					var platformContent = Assert.Single(platformBorder.Subviews);
					var mask = Assert.IsAssignableFrom<CAShapeLayer>(platformContent.Layer.Mask);
					Assert.NotNull(mask.Path);

					var maskPathBounds = mask.Path.PathBoundingBox;
					var actualBounds = new Rect(
						platformContent.Frame.X + mask.Frame.X + maskPathBounds.X,
						platformContent.Frame.Y + mask.Frame.Y + maskPathBounds.Y,
						maskPathBounds.Width,
						maskPathBounds.Height);

					bool hasExpectedInnerPath =
						Math.Abs(expectedBounds.X - actualBounds.X) <= EdgeTolerance &&
						Math.Abs(expectedBounds.Y - actualBounds.Y) <= EdgeTolerance &&
						Math.Abs(expectedBounds.Width - actualBounds.Width) <= EdgeTolerance &&
						Math.Abs(expectedBounds.Height - actualBounds.Height) <= EdgeTolerance;

					Assert.True(hasExpectedInnerPath,
						$"Polygon Border inner clip mismatch: expected native mask bounds {expectedBounds}, actual {actualBounds}; inset={BorderThickness}; tolerance={EdgeTolerance}.");
				});
		}

		static ResourceDictionary CreateResources()
		{
			var resources = new ResourceDictionary();
			resources.Add("BorderStyleCircle", CreateBorderStyle(new Ellipse()));
			resources.Add("BorderStyleRoundRectangle", CreateBorderStyle(new RoundRectangle()));
			resources.Add("BorderStyleTriangle", CreateBorderStyle(new Polygon
			{
				Points = new PointCollection
				{
					new Point(40, 10),
					new Point(70, 80),
					new Point(10, 50)
				},
				StrokeThickness = 3
			}));

			var labelStyle = new Style(typeof(Label));
			labelStyle.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#99FF0000") });
			labelStyle.Setters.Add(new Setter { Property = Label.FontSizeProperty, Value = 64d });
			labelStyle.Setters.Add(new Setter { Property = Label.HorizontalTextAlignmentProperty, Value = TextAlignment.Center });
			labelStyle.Setters.Add(new Setter { Property = Label.VerticalTextAlignmentProperty, Value = TextAlignment.Center });
			labelStyle.Setters.Add(new Setter { Property = View.HorizontalOptionsProperty, Value = LayoutOptions.Center });
			labelStyle.Setters.Add(new Setter { Property = View.VerticalOptionsProperty, Value = LayoutOptions.Center });
			resources.Add("ButtonIconStyle", labelStyle);
			return resources;
		}

		static Style CreateBorderStyle(Shape shape)
		{
			var style = new Style(typeof(Border));
			style.Setters.Add(new Setter { Property = Border.StrokeShapeProperty, Value = shape });
			style.Setters.Add(new Setter { Property = VisualElement.WidthRequestProperty, Value = BorderSize });
			style.Setters.Add(new Setter { Property = VisualElement.HeightRequestProperty, Value = BorderSize });
			style.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.LightBlue });
			style.Setters.Add(new Setter { Property = Border.StrokeThicknessProperty, Value = BorderThickness });
			style.Setters.Add(new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Colors.LightGreen) });
			return style;
		}

		static Border CreateBorder(ResourceDictionary resources, string styleKey, View content)
		{
			return new Border
			{
				Style = (Style)resources[styleKey],
				Content = content
			};
		}

		static Label CreateLabel(ResourceDictionary resources)
		{
			return new Label
			{
				Style = (Style)resources["ButtonIconStyle"],
				FontSize = 40,
				Text = "+",
				TextColor = Color.FromArgb("#0088ee")
			};
		}

		static Image CreateImage()
		{
			return new Image
			{
				Source = "oasis.jpg",
				Scale = 1
			};
		}

		static void AddToGrid(Grid grid, View view, int column, int row)
		{
			Grid.SetColumn(view, column);
			Grid.SetRow(view, row);
			grid.Children.Add(view);
		}

		static void AssertGridPosition(View view, int column, int row)
		{
			Assert.Equal(column, Grid.GetColumn(view));
			Assert.Equal(row, Grid.GetRow(view));
		}

		static void AssertImageConfigured(Image image)
		{
			Assert.NotNull(image.Source);
			Assert.Equal(1, image.Scale);
		}

		static UIView GetPlatformView(Border border)
		{
			Assert.NotNull(border.Handler);
			var platformView = Assert.IsType<Microsoft.Maui.Platform.ContentView>(border.Handler.PlatformView);
			return platformView;
		}

		static int CountGlyphPixels(RawBitmap bitmap)
		{
			int count = 0;
			ForEachPixel(bitmap, (r, g, b, a, _, _) =>
			{
				if (a > 128 && b > 150 && b > r + 40 && b > g + 30)
					count++;
			});
			return count;
		}

		static void ForEachPixel(RawBitmap bitmap, Action<byte, byte, byte, byte, double, double> action)
		{
			for (int row = 0; row < bitmap.PixelHeight; row++)
			{
				for (int column = 0; column < bitmap.PixelWidth; column++)
				{
					int index = (row * bitmap.PixelWidth + column) * 4;
					byte blue = bitmap.PixelBuffer[index];
					byte green = bitmap.PixelBuffer[index + 1];
					byte red = bitmap.PixelBuffer[index + 2];
					byte alpha = bitmap.PixelBuffer[index + 3];
					action(red, green, blue, alpha, (column + 0.5) / bitmap.Density, (row + 0.5) / bitmap.Density);
				}
			}
		}

		static Point[] InsetPolygon(Point[] polygon, double inset)
		{
			var offsetStarts = new Point[polygon.Length];
			var offsetEnds = new Point[polygon.Length];

			for (int i = 0; i < polygon.Length; i++)
			{
				Point start = polygon[i];
				Point end = polygon[(i + 1) % polygon.Length];
				double dx = end.X - start.X;
				double dy = end.Y - start.Y;
				double length = Math.Sqrt(dx * dx + dy * dy);
				double offsetX = -dy / length * inset;
				double offsetY = dx / length * inset;
				offsetStarts[i] = new Point(start.X + offsetX, start.Y + offsetY);
				offsetEnds[i] = new Point(end.X + offsetX, end.Y + offsetY);
			}

			var result = new Point[polygon.Length];
			for (int i = 0; i < polygon.Length; i++)
			{
				int previous = (i + polygon.Length - 1) % polygon.Length;
				result[i] = IntersectLines(offsetStarts[previous], offsetEnds[previous], offsetStarts[i], offsetEnds[i]);
			}

			return result;
		}

		static Rect GetBounds(Point[] polygon)
		{
			double left = double.MaxValue;
			double top = double.MaxValue;
			double right = double.MinValue;
			double bottom = double.MinValue;

			foreach (Point point in polygon)
			{
				left = Math.Min(left, point.X);
				top = Math.Min(top, point.Y);
				right = Math.Max(right, point.X);
				bottom = Math.Max(bottom, point.Y);
			}

			return new Rect(left, top, right - left, bottom - top);
		}

		static Point IntersectLines(Point firstStart, Point firstEnd, Point secondStart, Point secondEnd)
		{
			double firstX = firstEnd.X - firstStart.X;
			double firstY = firstEnd.Y - firstStart.Y;
			double secondX = secondEnd.X - secondStart.X;
			double secondY = secondEnd.Y - secondStart.Y;
			double denominator = firstX * secondY - firstY * secondX;
			double deltaX = secondStart.X - firstStart.X;
			double deltaY = secondStart.Y - firstStart.Y;
			double position = (deltaX * secondY - deltaY * secondX) / denominator;
			return new Point(firstStart.X + position * firstX, firstStart.Y + position * firstY);
		}

	}
}
#endif

