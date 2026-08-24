#if MACCATALYST
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Maps.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Map)]
	[Category("Issue37229")]
	public class Issue37229 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ClearResetsEveryMapElementIdAfterUpdatingOnePolyline()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<ContentView, ContentViewHandler>();
					handlers.AddMauiMaps();
				});
			});

			await InvokeOnMainThreadAsync(async () =>
			{
				var first = new Polyline
				{
					StrokeColor = Colors.Red,
					StrokeWidth = 4,
					Geopath =
					{
						new Location(47.600, -122.330),
						new Location(47.610, -122.330)
					}
				};

				var second = new Polyline
				{
					StrokeColor = Colors.Blue,
					StrokeWidth = 4,
					Geopath =
					{
						new Location(47.620, -122.340),
						new Location(47.630, -122.340)
					}
				};

				var map = new Map(MapSpan.FromCenterAndRadius(
					new Location(47.615, -122.335),
					Distance.FromKilometers(3)));
				map.MapElements.Add(first);
				map.MapElements.Add(second);

				var mapHost = new ContentView
				{
					MinimumHeightRequest = 300,
					Content = map
				};
				var grid = new Grid();
				grid.Add(mapHost);
				var page = new ContentPage { Content = grid };

				await CreateHandlerAndAddToWindow(page, async () =>
				{
					var mapHandler = Assert.IsType<MapHandler>(map.Handler);
					var platformView = Assert.IsType<MauiMKMapView>(mapHandler.PlatformView);

					await AssertEventually(
						() => platformView.Overlays.Length == 2 &&
							first.MapElementId is not null &&
							second.MapElementId is not null,
						message: "The attached map did not create both native polyline overlays.");

					Assert.Equal(2, platformView.Overlays.Length);
					Assert.NotSame(first.MapElementId, second.MapElementId);
					var originalFirstId = first.MapElementId;
					var originalSecondId = second.MapElementId;

					var geopathCountAfterAdd = -1;
					var geopathAction = (NotifyCollectionChangedAction)(-1);
					((INotifyCollectionChanged)first.Geopath).CollectionChanged += (_, args) =>
					{
						geopathAction = args.Action;
						geopathCountAfterAdd = first.Geopath.Count;
					};

					first.Geopath.Add(new Location(47.620, -122.330));

					Assert.Equal(NotifyCollectionChangedAction.Add, geopathAction);
					Assert.Equal(3, geopathCountAfterAdd);
					await AssertEventually(
						() => platformView.Overlays.Length == 2 &&
							first.MapElementId is not null &&
							!ReferenceEquals(first.MapElementId, originalFirstId) &&
							ReferenceEquals(second.MapElementId, originalSecondId),
						message: "Updating the first polyline did not replace only its native overlay.");

					Assert.Equal(2, platformView.Overlays.Length);
					var mapElementCountAfterReset = -1;
					var mapElementsAction = (NotifyCollectionChangedAction)(-1);
					((INotifyCollectionChanged)map.MapElements).CollectionChanged += (_, args) =>
					{
						mapElementsAction = args.Action;
						mapElementCountAfterReset = map.MapElements.Count;
					};

					map.MapElements.Clear();

					Assert.Equal(NotifyCollectionChangedAction.Reset, mapElementsAction);
					Assert.Equal(0, mapElementCountAfterReset);
					Assert.Empty(map.MapElements);
					Assert.Empty(platformView.Overlays);

					var idStates = $"first ID: {(first.MapElementId is null ? "null" : "non-null")}; second ID: {(second.MapElementId is null ? "null" : "non-null")}";
					Assert.True(first.MapElementId is null, $"MapElements.Clear did not reset the first polyline MapElementId; {idStates}");
					Assert.True(second.MapElementId is null, $"MapElements.Clear retained a stale MapElementId for the second polyline; {idStates}");
				});
			});
		}
	}
}
#endif

