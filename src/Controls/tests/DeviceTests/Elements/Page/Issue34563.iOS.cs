#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Page)]
	[Category("Issue34563")]
	public class Issue34563 : ControlsHandlerTestBase
	{
		const string MarkerText = "AFFECTED TOP CONTENT";
		const double PositionTolerance = 2;

		[Fact]
		public async Task ContainerChildRespectsTopSafeAreaWhenPageEdgesMismatch()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
				});
			});

			var controlScene = CreateScene(SafeAreaEdges.Container);
			await AssertMarkerPosition(controlScene.Page, controlScene.Marker, true);

			var affectedScene = CreateScene(new SafeAreaEdges(SafeAreaRegions.None, SafeAreaRegions.None, SafeAreaRegions.None, SafeAreaRegions.Container));
			await AssertMarkerPosition(affectedScene.Page, affectedScene.Marker, false);
		}

		async Task AssertMarkerPosition(ContentPage page, Label marker, bool isControl)
		{
			var observedTop = -1d;
			marker.SizeChanged += OnMarkerSizeChanged;

			try
			{
				await CreateHandlerAndAddToWindow<IWindowHandler>(page, async handler =>
				{
					await AssertEventually(
						() => observedTop >= 0,
						message: "Issue34563 marker did not receive an initial layout callback.");

					var nativeMarker = Assert.IsType<LabelHandler>(marker.Handler).PlatformView;
					await AssertEventually(
						() => nativeMarker.Window is not null && nativeMarker.Bounds.Height > 0,
						message: "Issue34563 marker was not rendered in the test window.");

					Assert.Equal(MarkerText, nativeMarker.Text);
					Assert.Equal(96, nativeMarker.Bounds.Height, PositionTolerance);

					var nativeWindow = nativeMarker.Window;
					Assert.Same(handler.PlatformView, nativeWindow);
					var nativeSafeAreaTop = nativeWindow.SafeAreaLayoutGuide.LayoutFrame.Top;
					Assert.True(
						nativeSafeAreaTop > 0,
						$"Issue34563 requires a nonzero native top safe-area inset, but nativeSafeAreaTop={nativeSafeAreaTop:F1}.");

					var markerTop = nativeMarker.ConvertRectToView(nativeMarker.Bounds, nativeWindow).Top;
					if (isControl)
					{
						Assert.InRange(markerTop, nativeSafeAreaTop - PositionTolerance, nativeSafeAreaTop + PositionTolerance);
						return;
					}

					Assert.True(
						markerTop + PositionTolerance >= nativeSafeAreaTop,
						$"Issue34563 affected top marker entered the unsafe area: markerTop={markerTop:F1}, nativeSafeAreaTop={nativeSafeAreaTop:F1}, rowHeight={nativeMarker.Bounds.Height:F1}, tolerance={PositionTolerance:F1}");
				});
			}
			finally
			{
				marker.SizeChanged -= OnMarkerSizeChanged;
			}

			void OnMarkerSizeChanged(object sender, EventArgs e)
			{
				observedTop = marker.Y;
			}
		}

		static (ContentPage Page, Label Marker) CreateScene(SafeAreaEdges pageSafeAreaEdges)
		{
			var marker = new Label
			{
				Text = MarkerText,
				BackgroundColor = Color.FromArgb("#D32F2F"),
				TextColor = Colors.White,
				FontAttributes = FontAttributes.Bold,
				FontSize = 20,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center
			};

			var explanatoryContent = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 14,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						Text = "Page: top=None, bottom=Container",
						TextColor = Color.FromArgb("#202020"),
						HorizontalTextAlignment = TextAlignment.Center
					},
					new Label
					{
						Text = "Child: SafeAreaEdges=Container",
						TextColor = Color.FromArgb("#202020"),
						HorizontalTextAlignment = TextAlignment.Center
					},
					new Label
					{
						Text = "The red child content should remain below the iOS status area.",
						TextColor = Color.FromArgb("#202020"),
						HorizontalTextAlignment = TextAlignment.Center
					}
				}
			};

			var grid = new Grid
			{
				SafeAreaEdges = SafeAreaEdges.Container,
				BackgroundColor = Color.FromArgb("#FFE082"),
				RowDefinitions =
				{
					new RowDefinition(96),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};

			grid.Add(marker, 0, 0);
			grid.Add(explanatoryContent, 0, 1);
			grid.Add(new Button
			{
				Text = "Check safe-area layout",
				Margin = new Thickness(24, 8)
			}, 0, 2);
			grid.Add(new Label
			{
				Text = "Safe-area layout status",
				BackgroundColor = Color.FromArgb("#263238"),
				TextColor = Colors.White,
				FontAttributes = FontAttributes.Bold,
				FontSize = 18,
				Padding = 16,
				HorizontalTextAlignment = TextAlignment.Center
			}, 0, 3);

			var page = new ContentPage
			{
				SafeAreaEdges = pageSafeAreaEdges,
				BackgroundColor = Color.FromArgb("#1B1B1B"),
				Content = grid
			};
			NavigationPage.SetHasNavigationBar(page, false);

			return (page, marker);
		}
	}
}
#endif

