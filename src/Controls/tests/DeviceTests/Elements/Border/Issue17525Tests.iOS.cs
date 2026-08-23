#if IOS && !MACCATALYST
using System;
using System.Linq;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Border)]
	[Category("Issue17525")]
	public class Issue17525 : ControlsHandlerTestBase
	{
		const double BorderSize = 101;
		const double BorderStrokeThickness = 8;

		[Fact]
		public async Task PolygonContentMaskRemainsInsideOuterStrokePath()
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

			var referenceScene = CreateScene(new RoundRectangle());
			double referenceMaskRight = await GetMaskRightInBorderCoordinates(referenceScene.Page, referenceScene.Border, referenceScene.Content);
			Assert.True(referenceMaskRight <= BorderSize + 0.5,
				$"Reference content mask crossed the outer stroke path: mask right {referenceMaskRight:F2}, outer right {BorderSize:F2}");

			var triangle = new Polygon
			{
				Points = new PointCollection
				{
					new Point(40, 10),
					new Point(70, 80),
					new Point(10, 50)
				},
				StrokeThickness = 3
			};
			var triangleScene = CreateScene(triangle);

			Assert.Same(triangle, triangleScene.Border.StrokeShape);
			Assert.Equal(3, triangle.Points.Count);
			Assert.Equal(new Point(40, 10), triangle.Points[0]);
			Assert.Equal(new Point(70, 80), triangle.Points[1]);
			Assert.Equal(new Point(10, 50), triangle.Points[2]);
			Assert.Equal(3d, triangle.StrokeThickness);
			Assert.Equal(BorderStrokeThickness, triangleScene.Border.StrokeThickness);

			double maskRight = await GetMaskRightInBorderCoordinates(
				triangleScene.Page,
				triangleScene.Border,
				triangleScene.Content);
			double outerPolygonRight = triangle.Points.Max(point => point.X);

			Assert.Equal(70d, outerPolygonRight);
			Assert.True(maskRight <= outerPolygonRight + 0.5,
				$"Polygon content mask crossed the outer stroke path: mask right {maskRight:F2}, outer right {outerPolygonRight:F2}");
		}

		async Task<double> GetMaskRightInBorderCoordinates(ContentPage page, Border border, Label content)
		{
			bool handlerAttachmentObserved = false;
			border.HandlerChanged += (_, _) =>
			{
				if (border.Handler is not null)
					handlerAttachmentObserved = true;
			};

			double maskRight = double.NaN;
			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var platformBorder = Assert.IsType<Microsoft.Maui.Platform.ContentView>(border.Handler.PlatformView);
				var platformLabel = Assert.IsAssignableFrom<UIView>(content.Handler.PlatformView);
				var platformContent = content.ToPlatform();

				await AssertionExtensions.AssertEventually(() =>
					handlerAttachmentObserved &&
					platformBorder.Window is not null &&
					platformBorder.Bounds.Width > 0 &&
					platformBorder.Bounds.Height > 0 &&
					platformContent.Layer.Mask is CAShapeLayer { Path: not null });

				Assert.True(handlerAttachmentObserved);
				Assert.InRange((double)platformBorder.Bounds.Width, BorderSize - 0.5, BorderSize + 0.5);
				Assert.InRange((double)platformBorder.Bounds.Height, BorderSize - 0.5, BorderSize + 0.5);
				Assert.Contains(platformBorder.Subviews, view => view.Handle == platformContent.Handle);
				Assert.True(platformContent.Handle == platformLabel.Handle || platformLabel.IsDescendantOfView(platformContent));
				Assert.NotEqual((nint)0, platformContent.Tag);

				var mask = Assert.IsAssignableFrom<CAShapeLayer>(platformContent.Layer.Mask);
				Assert.NotNull(mask.Path);
				Assert.Equal(mask.Handle, platformContent.Layer.Mask.Handle);

				CGRect pathBounds = mask.Path.PathBoundingBox;
				Assert.False(pathBounds.IsEmpty);
				double pathRightInContent = mask.Frame.X + pathBounds.X + pathBounds.Width - mask.Bounds.X;
				maskRight = platformContent.ConvertPointToView(
					new CGPoint(pathRightInContent, 0),
					platformBorder).X;
			});

			Assert.False(double.IsNaN(maskRight));
			return maskRight;
		}

		static (ContentPage Page, Border Border, Label Content) CreateScene(IShape strokeShape)
		{
			var borderStyle = new Style(typeof(Border))
			{
				Setters =
				{
					new Setter { Property = Border.StrokeShapeProperty, Value = strokeShape },
					new Setter { Property = VisualElement.WidthRequestProperty, Value = BorderSize },
					new Setter { Property = VisualElement.HeightRequestProperty, Value = BorderSize },
					new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.LightBlue },
					new Setter { Property = Border.StrokeThicknessProperty, Value = BorderStrokeThickness },
					new Setter { Property = Border.StrokeProperty, Value = Colors.LightGreen }
				}
			};
			var labelStyle = new Style(typeof(Label))
			{
				Setters =
				{
					new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#99FF0000") },
					new Setter { Property = Label.FontSizeProperty, Value = 64d },
					new Setter { Property = Label.HorizontalTextAlignmentProperty, Value = TextAlignment.Center },
					new Setter { Property = Label.VerticalTextAlignmentProperty, Value = TextAlignment.Center },
					new Setter { Property = View.HorizontalOptionsProperty, Value = LayoutOptions.Center },
					new Setter { Property = View.VerticalOptionsProperty, Value = LayoutOptions.Center }
				}
			};
			var content = new Label
			{
				Style = labelStyle,
				FontSize = 40,
				Text = "+",
				TextColor = Color.FromArgb("#0088ee")
			};
			var border = new Border
			{
				Style = borderStyle,
				Content = content
			};
			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Star }
				},
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Star }
				},
				ColumnSpacing = 10,
				RowSpacing = 10,
				VerticalOptions = LayoutOptions.Center
			};
			grid.Add(border, 0, 2);

			var stack = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 16,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						Text = "Triangle Border from BorderResizeContent",
						FontAttributes = FontAttributes.Bold,
						HorizontalTextAlignment = TextAlignment.Center
					},
					grid
				}
			};
			var page = new ContentPage
			{
				Title = "Polygon Border inner path",
				Content = stack
			};
			page.Resources.Add("BorderStyleTriangle", borderStyle);
			page.Resources.Add("ButtonIconStyle", labelStyle);

			return (page, border, content);
		}
	}
}
#endif

