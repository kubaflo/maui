#if MACCATALYST
using System;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Window)]
	[Category("Issue27304")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue27304 : ControlsHandlerTestBase
	{
		const double RequestedX = 120;
		const double RequestedY = 140;
		const double RequestedWidth = 500;
		const double RequestedHeight = 400;
		const double GeometryTolerance = 2;

		[Fact]
		public async Task InitialWindowGeometryIsApplied()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<NavigationPage, NavigationRenderer>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var targetWindow = await InvokeOnMainThreadAsync(() =>
			{
				var window = CreateTestWindow();
				window.X = RequestedX;
				window.Y = RequestedY;
				window.Width = RequestedWidth;
				window.Height = RequestedHeight;
				return window;
			});

			var targetNavigationPage = Assert.IsType<NavigationPage>(targetWindow.Page);
			var targetPage = Assert.IsType<ContentPage>(targetNavigationPage.RootPage);
			var targetGrid = Assert.IsType<Grid>(targetPage.Content);
			Assert.Collection(
				targetGrid.Children,
				child => Assert.IsType<Label>(child),
				child => Assert.IsType<Label>(child),
				child => Assert.IsType<Label>(child),
				child => Assert.IsType<Button>(child),
				child => Assert.IsType<Label>(child));
			Assert.Equal(RequestedX, targetWindow.X);
			Assert.Equal(RequestedY, targetWindow.Y);
			Assert.Equal(RequestedWidth, targetWindow.Width);
			Assert.Equal(RequestedHeight, targetWindow.Height);

			await CreateHandlerAndAddToWindow<IWindowHandler>(targetWindow, handler =>
			{
				var targetScene = handler.PlatformView.WindowScene;
				Assert.NotNull(targetScene);

				AssertFrame(
					targetScene.EffectiveGeometry.SystemFrame,
					RequestedX,
					RequestedY,
					RequestedWidth,
					RequestedHeight);
			});
		}

		static Window CreateTestWindow()
		{
			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 16,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
				}
			};

			grid.Add(
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 22,
					Text = "Initial Mac Catalyst window geometry",
				},
				0,
				0);
			grid.Add(new Label { Text = "Requested geometry: X=120, Y=140, Width=500, Height=400" }, 0, 1);
			grid.Add(new Label { Text = "Actual geometry: waiting for the window" }, 0, 2);
			grid.Add(
				new Button
				{
					IsEnabled = false,
					Text = "Check Initial Window Geometry",
				},
				0,
				3);
			grid.Add(
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					Text = "Geometry result pending",
					VerticalOptions = LayoutOptions.Start,
				},
				0,
				4);

			return new Window(
				new NavigationPage(
					new ContentPage
					{
						Title = "Window startup geometry",
						Content = grid,
					}));
		}

		static bool FrameMatches(
			CGRect frame,
			double expectedX,
			double expectedY,
			double expectedWidth,
			double expectedHeight) =>
			Math.Abs(frame.X - expectedX) <= GeometryTolerance &&
			Math.Abs(frame.Y - expectedY) <= GeometryTolerance &&
			Math.Abs(frame.Width - expectedWidth) <= GeometryTolerance &&
			Math.Abs(frame.Height - expectedHeight) <= GeometryTolerance;

		static void AssertFrame(
			CGRect frame,
			double expectedX,
			double expectedY,
			double expectedWidth,
			double expectedHeight) =>
			Assert.True(
				FrameMatches(frame, expectedX, expectedY, expectedWidth, expectedHeight),
				"Initial Mac Catalyst window geometry was not applied");
	}
}
#endif

