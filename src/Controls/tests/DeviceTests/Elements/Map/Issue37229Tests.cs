using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Maps;
using Xunit;

using Map = Microsoft.Maui.Controls.Maps.Map;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Category(TestCategory.Map)]
	[Category("Issue37229")]
	public class Issue37229 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ClearingAfterUpdatingOnePolylineResetsEveryMapElementId()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Map, Microsoft.Maui.Maps.Handlers.MapHandler>();
				});
			});

			var map = new Map(new MapSpan(new Location(47.615, -122.335), 0.06, 0.04));
			var first = new Polyline
			{
				StrokeColor = Colors.Red,
				StrokeWidth = 4,
				Geopath =
				{
					new Location(47.60, -122.33),
					new Location(47.61, -122.33)
				}
			};
			var second = new Polyline
			{
				StrokeColor = Colors.Blue,
				StrokeWidth = 4,
				Geopath =
				{
					new Location(47.62, -122.34),
					new Location(47.63, -122.34)
				}
			};

			map.MapElements.Add(first);
			map.MapElements.Add(second);

			var mapHost = new Grid
			{
				MinimumHeightRequest = 320,
				Children = { map }
			};
			var rootGrid = new Grid
			{
				Padding = 24,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				},
				Children = { mapHost }
			};
			Grid.SetRow(mapHost, 2);

			var page = new ContentPage
			{
				Content = rootGrid
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(
					() => InvokeOnMainThreadAsync(() =>
					{
						if (map.Handler is not Microsoft.Maui.Maps.Handlers.MapHandler handler)
							return false;

						var overlays = handler.PlatformView.Overlays;
						return overlays is { Length: 2 } &&
							first.MapElementId is not null &&
							second.MapElementId is not null &&
							!object.ReferenceEquals(first.MapElementId, second.MapElementId) &&
							System.Array.Exists(overlays, overlay => object.ReferenceEquals(overlay, first.MapElementId)) &&
							System.Array.Exists(overlays, overlay => object.ReferenceEquals(overlay, second.MapElementId));
					}),
					timeout: 10000,
					interval: 100,
					message: "Timed out waiting for both polylines to have distinct native overlays.");

				var mapHandler = Assert.IsType<Microsoft.Maui.Maps.Handlers.MapHandler>(map.Handler);
				var initialFirstMapElementId = first.MapElementId;
				var updateObserved = false;

				await InvokeOnMainThreadAsync(() => first.Geopath.Add(new Location(47.62, -122.33)));

				await AssertEventually(
					() => InvokeOnMainThreadAsync(() =>
					{
						var overlays = mapHandler.PlatformView.Overlays;
						updateObserved = first.MapElementId is not null &&
							!object.ReferenceEquals(initialFirstMapElementId, first.MapElementId) &&
							overlays is { Length: 2 } &&
							System.Array.Exists(overlays, overlay => object.ReferenceEquals(overlay, first.MapElementId)) &&
							System.Array.Exists(overlays, overlay => object.ReferenceEquals(overlay, second.MapElementId));
						return updateObserved;
					}),
					timeout: 10000,
					interval: 100,
					message: "Timed out waiting for the first polyline update to replace its native overlay.");
				Assert.True(updateObserved, "The first polyline update did not produce a new native overlay identity.");

				await InvokeOnMainThreadAsync(() => map.MapElements.Clear());

				await AssertEventually(
					() => InvokeOnMainThreadAsync(() =>
						map.MapElements.Count == 0 && mapHandler.PlatformView.Overlays is { Length: 0 }),
					timeout: 10000,
					interval: 100,
					message: "Timed out waiting for the managed map elements and native overlays to clear.");

				var firstMapElementIdAfterClear = await InvokeOnMainThreadAsync(() => first.MapElementId);
				var secondMapElementIdAfterClear = await InvokeOnMainThreadAsync(() => second.MapElementId);

				Assert.True(firstMapElementIdAfterClear is null,
					$"First polyline retained stale MapElementId after clear: expected null, observed {firstMapElementIdAfterClear}");
				Assert.True(secondMapElementIdAfterClear is null,
					$"Second polyline retained stale MapElementId after clear: expected null, observed {secondMapElementIdAfterClear}");
			});
		}
	}
#endif
}

