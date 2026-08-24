#if WINDOWS
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WButtonAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer;
using WInvokeProvider = Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider;
using WPatternInterface = Microsoft.UI.Xaml.Automation.Peers.PatternInterface;
using WVisibility = Microsoft.UI.Xaml.Visibility;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue17525")]
	public class Issue17525 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task PolygonBorderContentUsesInnerPathAfterBecomingVisible()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var labelStyle = new Style(typeof(Label))
			{
				Setters =
				{
					new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#99FF0000") },
					new Setter { Property = Label.FontSizeProperty, Value = 40d },
					new Setter { Property = Label.HorizontalTextAlignmentProperty, Value = TextAlignment.Center },
					new Setter { Property = Label.VerticalTextAlignmentProperty, Value = TextAlignment.Center },
					new Setter { Property = View.HorizontalOptionsProperty, Value = LayoutOptions.Center },
					new Setter { Property = View.VerticalOptionsProperty, Value = LayoutOptions.Center },
				}
			};

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

			var borderStyle = new Style(typeof(Border))
			{
				Setters =
				{
					new Setter { Property = Border.StrokeShapeProperty, Value = polygon },
					new Setter { Property = VisualElement.WidthRequestProperty, Value = 101d },
					new Setter { Property = VisualElement.HeightRequestProperty, Value = 101d },
					new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.LightBlue },
					new Setter { Property = Border.StrokeThicknessProperty, Value = 8d },
					new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Colors.LightGreen) },
				}
			};

			var triangleBorder = new Border
			{
				Style = borderStyle,
				IsVisible = false,
				Content = new Label
				{
					Style = labelStyle,
					Text = "+",
					TextColor = Color.FromArgb("#0088ee"),
				},
			};

			var showButton = new Button { Text = "Show reported triangle" };
			var checkButton = new Button { Text = "Check inner path", IsVisible = false };
			showButton.Clicked += (_, _) =>
			{
				triangleBorder.IsVisible = true;
				showButton.IsEnabled = false;
			};

			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star),
				},
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
				},
				ColumnSpacing = 10,
				RowSpacing = 10,
				VerticalOptions = LayoutOptions.Center,
			};
			grid.Add(triangleBorder, 0, 2);

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label { Text = "Issue 17525: Polygon Border clipping", FontSize = 20 },
					showButton,
					grid,
					checkButton,
				},
			};

			var page = new ContentPage
			{
				Title = "Polygon Border inner path",
				Content = layout,
				Resources =
				{
					{ "BorderStyleTriangle", borderStyle },
					{ "ButtonIconStyle", labelStyle },
				},
			};

			double observedPostRevealWidth = -1;
			var postRevealSizeChanged = false;
			var revealRequested = false;
			var checkInvoked = false;
			var observedInnerPath = -1;
			triangleBorder.SizeChanged += (_, _) =>
			{
				if (revealRequested && triangleBorder.Width > 0 && triangleBorder.Height > 0)
				{
					observedPostRevealWidth = triangleBorder.Width;
					postRevealSizeChanged = true;
					checkButton.IsVisible = true;
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.Same(layout, page.Content);
				Assert.Contains(triangleBorder, grid.Children);
				Assert.Same(borderStyle, triangleBorder.Style);
				Assert.Same(polygon, triangleBorder.StrokeShape);
				Assert.Equal(new Point(40, 10), polygon.Points[0]);
				Assert.Equal(new Point(70, 80), polygon.Points[1]);
				Assert.Equal(new Point(10, 50), polygon.Points[2]);
				Assert.Equal(3, polygon.StrokeThickness);
				Assert.Equal(101, triangleBorder.WidthRequest);
				Assert.Equal(101, triangleBorder.HeightRequest);
				Assert.Equal(Colors.LightBlue, triangleBorder.BackgroundColor);
				Assert.Equal(8, triangleBorder.StrokeThickness);
				Assert.Equal(Colors.LightGreen, Assert.IsType<SolidColorBrush>(triangleBorder.Stroke).Color);
				Assert.Equal(2, Grid.GetRow(triangleBorder));
				Assert.Equal(0, Grid.GetColumn(triangleBorder));

				var triangleLabel = Assert.IsType<Label>(triangleBorder.Content);
				Assert.Equal("+", triangleLabel.Text);
				Assert.Equal(40, triangleLabel.FontSize);
				Assert.Equal(TextAlignment.Center, triangleLabel.HorizontalTextAlignment);
				Assert.Equal(TextAlignment.Center, triangleLabel.VerticalTextAlignment);
				Assert.Equal(LayoutOptions.Center, triangleLabel.HorizontalOptions);
				Assert.Equal(LayoutOptions.Center, triangleLabel.VerticalOptions);
				Assert.Equal(Color.FromArgb("#0088ee"), triangleLabel.TextColor);

				var borderHandler = Assert.IsType<BorderHandler>(triangleBorder.Handler);
				var contentPanel = borderHandler.PlatformView;
				Assert.NotNull(contentPanel.Content);
				checkButton.Clicked += (_, _) =>
				{
					checkInvoked = true;
					observedInnerPath = contentPanel.IsInnerPath ? 1 : 0;
				};

				var buttonHandler = Assert.IsType<ButtonHandler>(showButton.Handler);
				var automationPeer = new WButtonAutomationPeer(buttonHandler.PlatformView);
				var invokeProvider = Assert.IsAssignableFrom<WInvokeProvider>(
					automationPeer.GetPattern(WPatternInterface.Invoke));

				revealRequested = true;
				invokeProvider.Invoke();

				await AssertEventually(() =>
					postRevealSizeChanged &&
					triangleBorder.IsVisible &&
					contentPanel.Visibility == WVisibility.Visible &&
					contentPanel.ActualWidth > 0 &&
					contentPanel.ActualHeight > 0 &&
					checkButton.IsVisible);

				Assert.True(postRevealSizeChanged);
				Assert.True(observedPostRevealWidth > 0);
				Assert.True(triangleBorder.IsVisible);
				Assert.Equal(WVisibility.Visible, contentPanel.Visibility);
				Assert.True(contentPanel.ActualWidth > 0);
				Assert.True(contentPanel.ActualHeight > 0);

				var checkButtonHandler = Assert.IsType<ButtonHandler>(checkButton.Handler);
				var checkAutomationPeer = new WButtonAutomationPeer(checkButtonHandler.PlatformView);
				var checkInvokeProvider = Assert.IsAssignableFrom<WInvokeProvider>(
					checkAutomationPeer.GetPattern(WPatternInterface.Invoke));
				checkInvokeProvider.Invoke();

				await AssertEventually(() => checkInvoked);
				Assert.True(checkInvoked);
				Assert.NotEqual(-1, observedInnerPath);
				Assert.True(observedInnerPath == 1,
					$"Polygon Border native content clip must use an inner path; observed IsInnerPath={observedInnerPath == 1}, expected=True; native frame={contentPanel.ActualWidth:F2}x{contentPanel.ActualHeight:F2}, stroke={triangleBorder.StrokeThickness:F2}");
			});
		}
	}
}
#endif

