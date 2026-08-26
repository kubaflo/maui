#if WINDOWS
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WPath = Microsoft.UI.Xaml.Shapes.Path;
using WPenLineCap = Microsoft.UI.Xaml.Media.PenLineCap;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29741")]
	public class Issue29741 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task StrokeLineCapMapsToNativeStrokeDashCap()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Border, BorderHandler>();
				});
			});

			var flatBorder = CreateBorder(PenLineCap.Flat);
			var roundBorder = CreateBorder(PenLineCap.Round);
			var squareBorder = CreateBorder(PenLineCap.Square);
			var layout = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 8,
				HorizontalOptions = LayoutOptions.Center
			};

			layout.Add(new Label
			{
				Text = "Dashed Border StrokeLineCap comparison",
				FontSize = 20,
				FontAttributes = FontAttributes.Bold
			});
			layout.Add(new Label { Text = "Flat" });
			layout.Add(flatBorder);
			layout.Add(new Label { Text = "Round" });
			layout.Add(roundBorder);
			layout.Add(new Label { Text = "Square" });
			layout.Add(squareBorder);
			layout.Add(new Button { Text = "Check rendered line caps" });
			layout.Add(new Label
			{
				Text = "Native dash cap inspection",
				FontSize = 18,
				FontAttributes = FontAttributes.Bold
			});

			var page = new ContentPage
			{
				Title = "StrokeLineCap",
				Content = new ScrollView { Content = layout }
			};

			var loaded = false;
			var inspected = false;
			var roundObserved = (WPenLineCap)(-1);
			var squareObserved = (WPenLineCap)(-1);
			page.Loaded += (_, _) => loaded = true;

			await AttachAndRun<PageHandler>(page, async _ =>
			{
				await AssertEventually(() =>
					loaded &&
					FindPaths(flatBorder.Handler?.PlatformView as WDependencyObject).Count == 1 &&
					FindPaths(roundBorder.Handler?.PlatformView as WDependencyObject).Count == 1 &&
					FindPaths(squareBorder.Handler?.PlatformView as WDependencyObject).Count == 1);

				Assert.NotNull(flatBorder.Handler);
				Assert.NotNull(roundBorder.Handler);
				Assert.NotNull(squareBorder.Handler);

				var flatPlatformView = Assert.IsAssignableFrom<WDependencyObject>(flatBorder.Handler.PlatformView);
				var roundPlatformView = Assert.IsAssignableFrom<WDependencyObject>(roundBorder.Handler.PlatformView);
				var squarePlatformView = Assert.IsAssignableFrom<WDependencyObject>(squareBorder.Handler.PlatformView);
				var flatPath = Assert.Single(FindPaths(flatPlatformView));
				var roundPath = Assert.Single(FindPaths(roundPlatformView));
				var squarePath = Assert.Single(FindPaths(squarePlatformView));

				Assert.Equal(PenLineCap.Flat, flatBorder.StrokeLineCap);
				Assert.Equal(PenLineCap.Round, roundBorder.StrokeLineCap);
				Assert.Equal(PenLineCap.Square, squareBorder.StrokeLineCap);
				AssertDashArray(flatBorder);
				AssertDashArray(roundBorder);
				AssertDashArray(squareBorder);
				Assert.Equal(WPenLineCap.Flat, flatPath.StrokeDashCap);

				roundObserved = roundPath.StrokeDashCap;
				squareObserved = squarePath.StrokeDashCap;
				inspected = true;
			});

			Assert.True(loaded);
			Assert.True(inspected);
			Assert.NotEqual((WPenLineCap)(-1), roundObserved);
			Assert.NotEqual((WPenLineCap)(-1), squareObserved);
			Assert.True(
				roundObserved == WPenLineCap.Round && squareObserved == WPenLineCap.Square,
				$"Issue29741 native StrokeDashCap mismatch: Round observed {roundObserved} expected {WPenLineCap.Round}; Square observed {squareObserved} expected {WPenLineCap.Square}.");
		}

		static Border CreateBorder(PenLineCap lineCap) =>
			new Border
			{
				BackgroundColor = Colors.Gray,
				HeightRequest = 58,
				HorizontalOptions = LayoutOptions.Center,
				Stroke = Colors.Blue,
				StrokeDashArray = new DoubleCollection { 5, 3 },
				StrokeLineCap = lineCap,
				StrokeThickness = 10,
				WidthRequest = 320
			};

		static void AssertDashArray(Border border)
		{
			Assert.Collection(
				border.StrokeDashArray,
				value => Assert.Equal(5, value),
				value => Assert.Equal(3, value));
		}

		static IReadOnlyList<WPath> FindPaths(WDependencyObject parent)
		{
			var paths = new List<WPath>();
			if (parent is not null)
				AddPaths(parent, paths);

			return paths;
		}

		static void AddPaths(WDependencyObject parent, List<WPath> paths)
		{
			var childCount = WVisualTreeHelper.GetChildrenCount(parent);
			for (var index = 0; index < childCount; index++)
			{
				var child = WVisualTreeHelper.GetChild(parent, index);
				if (child is WPath path)
					paths.Add(path);

				AddPaths(child, paths);
			}
		}
	}
}
#endif

