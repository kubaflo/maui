#if MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Maps.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
#endif

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Category(TestCategory.Map)]
	[Category("Issue37229")]
	public class Issue37229 : ControlsHandlerTestBase
	{
		void SetupBuilder()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Map, MapHandler>();
				});
			});
		}

		[Fact]
		public async Task UpdatingOnePolylineThenClearingResetsAllMapElementIds()
		{
			SetupBuilder();

			var firstPolyline = new Polyline
			{
				StrokeColor = Colors.Red,
				StrokeWidth = 4,
				Geopath =
				{
					new Location(47.60, -122.33),
					new Location(47.61, -122.33),
				}
			};

			var secondPolyline = new Polyline
			{
				StrokeColor = Colors.Blue,
				StrokeWidth = 4,
				Geopath =
				{
					new Location(47.62, -122.34),
					new Location(47.63, -122.34),
				}
			};

			var map = new Map();
			map.MapElements.Add(firstPolyline);
			map.MapElements.Add(secondPolyline);

			var mapHost = new ContentView
			{
				Content = map
			};

			var grid = new Grid
			{
				Padding = 16,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star)
				}
			};
			grid.Add(mapHost);

			var page = new ContentPage
			{
				Content = grid
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var mapHandler = Assert.IsType<MapHandler>(map.Handler);
				var platformView = Assert.IsType<MauiMKMapView>(mapHandler.PlatformView);

				Assert.NotNull(firstPolyline.MapElementId);
				Assert.NotNull(secondPolyline.MapElementId);
				var initialOverlays = platformView.Overlays;
				Assert.NotNull(initialOverlays);
				Assert.Equal(2, initialOverlays.Length);
				Assert.Contains(initialOverlays, overlay => ReferenceEquals(overlay, firstPolyline.MapElementId));
				Assert.Contains(initialOverlays, overlay => ReferenceEquals(overlay, secondPolyline.MapElementId));

				var initialFirstId = firstPolyline.MapElementId;
				var initialSecondId = secondPolyline.MapElementId;
				var updateToken = -1;

				firstPolyline.Geopath.Add(new Location(47.62, -122.33));

				await AssertEventually(
					() =>
					{
						if (firstPolyline.MapElementId is null ||
							ReferenceEquals(firstPolyline.MapElementId, initialFirstId) ||
							!ReferenceEquals(secondPolyline.MapElementId, initialSecondId))
						{
							return false;
						}

						updateToken = 1;
						return true;
					},
					message: "The first polyline native overlay was not replaced.");

				Assert.Equal(1, updateToken);
				var updatedOverlays = platformView.Overlays;
				Assert.NotNull(updatedOverlays);
				Assert.Equal(2, updatedOverlays.Length);
				Assert.Contains(updatedOverlays, overlay => ReferenceEquals(overlay, firstPolyline.MapElementId));
				Assert.Contains(updatedOverlays, overlay => ReferenceEquals(overlay, secondPolyline.MapElementId));

				var clearToken = -1;
				map.MapElements.Clear();

				await AssertEventually(
					() =>
					{
						if (platformView.Overlays?.Length != 0 || firstPolyline.MapElementId is not null)
							return false;

						clearToken = 1;
						return true;
					},
					message: "The native overlays and updated polyline identifier were not cleared.");

				Assert.Equal(1, clearToken);
				Assert.True(
					secondPolyline.MapElementId is null,
					$"Second polyline MapElementId after clear: expected null; observed non-null; native type: {secondPolyline.MapElementId?.GetType().FullName}");
			});
		}
	}
#endif
}

