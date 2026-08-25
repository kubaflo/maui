#if MACCATALYST
using System;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue27304")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue27304 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task InitialWindowGeometryIsApplied()
		{
			const double requestedX = 100;
			const double requestedY = 100;
			const double requestedWidth = 800;
			const double requestedHeight = 600;
			const double tolerance = 1;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<NavigationPage, NavigationRenderer>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var headingLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 24,
				Text = "Issue 27304: Initial Window Geometry"
			};
			var requestedGeometryLabel = new Label
			{
				Text = "Requested: X=100, Y=100, Width=800, Height=600"
			};
			var observedGeometryLabel = new Label
			{
				Text = "Observed: waiting for window layout"
			};
			var readyLabel = new Label
			{
				IsVisible = false,
				Text = "Window geometry ready"
			};
			var checkGeometryButton = new Button
			{
				Text = "Check initial window geometry"
			};
			var contentLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					headingLabel,
					requestedGeometryLabel,
					observedGeometryLabel,
					readyLabel,
					checkGeometryButton
				}
			};
			var contentPage = new ContentPage { Content = contentLayout };
			var navigationPage = new NavigationPage(contentPage);
			var testWindow = new Window(navigationPage)
			{
				X = requestedX,
				Y = requestedY,
				Width = requestedWidth,
				Height = requestedHeight
			};

			var handlerSeen = false;
			var loadedSeen = false;
			CGRect? callbackFrame = null;
			IDisposable geometryObserver = null;

			testWindow.HandlerChanged += (_, _) =>
			{
				handlerSeen = true;
				if (testWindow.Handler?.PlatformView is UIWindow platformWindow &&
					platformWindow.WindowScene is UIWindowScene windowScene)
				{
					geometryObserver = windowScene.AddObserver(
						"effectiveGeometry",
						NSKeyValueObservingOptions.New,
						change =>
						{
							if (change.NewValue is UIWindowSceneGeometry geometry)
								callbackFrame = geometry.SystemFrame;
						});
				}
			};
			contentPage.Loaded += (_, _) => loadedSeen = true;

			try
			{
				await CreateHandlerAndAddToWindow<IWindowHandler>(testWindow, async handler =>
				{
					await AssertEventually(
						() => handlerSeen,
						timeout: 5000,
						message: "The Window handler was not created.");
					await AssertEventually(
						() => loadedSeen,
						timeout: 5000,
						message: "The ContentPage did not load.");
					Assert.Same(navigationPage, testWindow.Page);
					Assert.Same(contentPage, navigationPage.CurrentPage);
					Assert.Contains(headingLabel, contentLayout.Children);
					Assert.Contains(requestedGeometryLabel, contentLayout.Children);

					var platformWindow = Assert.IsType<UIWindow>(handler.PlatformView);
					var windowScene = Assert.IsType<UIWindowScene>(platformWindow.WindowScene);

					var nativeLayout = Assert.IsAssignableFrom<UIView>(contentLayout.Handler.PlatformView);
					var nativeContent = Assert.IsAssignableFrom<UIView>(contentPage.Handler.PlatformView);
					Assert.True(
						IsFinitePositive(nativeLayout.Bounds),
						"The expected content layout did not receive finite positive native bounds.");
					Assert.True(
						Matches(nativeLayout.Frame.Width, nativeContent.Bounds.Width, tolerance) &&
						Matches(nativeLayout.Frame.Height, nativeContent.Bounds.Height, tolerance),
						"The content layout did not fill its native page content bounds.");

					await AssertEventually(
						() => callbackFrame.HasValue,
						timeout: 5000,
						message: "The UIWindowScene effective-geometry callback was not observed.");

					var actualFrame = windowScene.EffectiveGeometry.SystemFrame;
					var geometryMatches =
						Matches(actualFrame.X, requestedX, tolerance) &&
						Matches(actualFrame.Y, requestedY, tolerance) &&
						Matches(actualFrame.Width, requestedWidth, tolerance) &&
						Matches(actualFrame.Height, requestedHeight, tolerance);

					Assert.True(
						geometryMatches,
						$"Initial Mac Catalyst window geometry was not applied: actual X={actualFrame.X:0.##}, Y={actualFrame.Y:0.##}, Width={actualFrame.Width:0.##}, Height={actualFrame.Height:0.##}; requested X={requestedX:0.##}, Y={requestedY:0.##}, Width={requestedWidth:0.##}, Height={requestedHeight:0.##}; tolerance={tolerance:0.##}.");
				});
			}
			finally
			{
				geometryObserver?.Dispose();
			}

			static bool Matches(double actual, double expected, double tolerance) =>
				Math.Abs(actual - expected) <= tolerance;

			static bool IsFinitePositive(CGRect frame) =>
				double.IsFinite(frame.X) &&
				double.IsFinite(frame.Y) &&
				double.IsFinite(frame.Width) &&
				double.IsFinite(frame.Height) &&
				frame.Width > 0 &&
				frame.Height > 0;
		}
	}
}
#endif

