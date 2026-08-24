#if MACCATALYST
using System.Linq;
using System.Threading.Tasks;
using MapKit;
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
	[Category(TestCategory.Map, "Issue37229")]
	public class Issue37229 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task UpdatingOnePolylineThenClearingResetsEveryMapElementId()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddMauiMaps();
				});
			});

			var first = new Polyline
			{
				StrokeColor = Colors.Red,
				StrokeWidth = 4
			};
			first.Geopath.Add(new Location(47.60, -122.33));
			first.Geopath.Add(new Location(47.61, -122.33));

			var second = new Polyline
			{
				StrokeColor = Colors.Blue,
				StrokeWidth = 4
			};
			second.Geopath.Add(new Location(47.62, -122.34));
			second.Geopath.Add(new Location(47.63, -122.34));

			var map = new Microsoft.Maui.Controls.Maps.Map(
				new MapSpan(new Location(47.615, -122.335), 0.05, 0.05));
			map.MapElements.Add(first);
			map.MapElements.Add(second);

			var mapHost = new ContentView
			{
				MinimumHeightRequest = 350,
				Content = map
			};
			var grid = new Grid
			{
				Padding = 16,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};

			grid.Add(new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 18,
				Text = "Two polylines before clear"
			}, 0, 0);
			grid.Add(mapHost, 0, 1);
			grid.Add(new Label { Text = "Ready: both elements are tracked" }, 0, 2);
			grid.Add(new Button { Text = "Mutate first polyline and clear" }, 0, 3);
			grid.Add(new VerticalStackLayout
			{
				Spacing = 4,
				Children =
				{
					new Label { Text = "Before clear: first ID set; second ID set" },
					new Label { FontAttributes = FontAttributes.Bold, Text = "Clear has not run" }
				}
			}, 0, 4);

			var page = new ContentPage { Content = grid };

			await AttachAndRun(page, async _ =>
			{
				var handler = Assert.IsType<MapHandler>(map.Handler);
				var nativeMap = Assert.IsType<MauiMKMapView>(handler.PlatformView);

				await AssertEventually(
					() => InvokeOnMainThreadAsync(() => first.MapElementId is MKPolyline),
					timeout: 10000,
					interval: 100,
					message: "Timed out waiting for the first polyline's native identity.");
				await AssertEventually(
					() => InvokeOnMainThreadAsync(() => second.MapElementId is MKPolyline),
					timeout: 10000,
					interval: 100,
					message: "Timed out waiting for the second polyline's native identity.");
				await AssertEventually(
					() => InvokeOnMainThreadAsync(() => nativeMap.Overlays?.Length == 2),
					timeout: 10000,
					interval: 100,
					message: "Timed out waiting for both native overlays.");

				var initialIds = await InvokeOnMainThreadAsync(() => (
					First: Assert.IsType<MKPolyline>(first.MapElementId),
					Second: Assert.IsType<MKPolyline>(second.MapElementId)));
				object observedUpdatedFirstId = null;

				await InvokeOnMainThreadAsync(
					() => first.Geopath.Add(new Location(47.62, -122.33)));

				await AssertEventually(
					() => InvokeOnMainThreadAsync(() =>
					{
						observedUpdatedFirstId = first.MapElementId;
						return observedUpdatedFirstId is MKPolyline &&
							!ReferenceEquals(observedUpdatedFirstId, initialIds.First);
					}),
					timeout: 10000,
					interval: 100,
					message: "Timed out waiting for the first polyline's native identity to be replaced.");
				await AssertEventually(
					() => InvokeOnMainThreadAsync(
						() => ReferenceEquals(second.MapElementId, initialIds.Second)),
					timeout: 10000,
					interval: 100,
					message: "The second polyline did not retain its native identity during the first update.");
				await AssertEventually(
					() => InvokeOnMainThreadAsync(() =>
						nativeMap.Overlays?.Length == 2 &&
						nativeMap.Overlays.Contains((MKPolyline)observedUpdatedFirstId) &&
						nativeMap.Overlays.Contains(initialIds.Second)),
					timeout: 10000,
					interval: 100,
					message: "Timed out waiting for the updated and untouched native overlays.");

				await InvokeOnMainThreadAsync(() => map.MapElements.Clear());

				await AssertEventually(
					() => InvokeOnMainThreadAsync(() => map.MapElements.Count == 0),
					timeout: 5000,
					interval: 100,
					message: "Timed out waiting for the managed map elements to clear.");
				await AssertEventually(
					() => InvokeOnMainThreadAsync(
						() => nativeMap.Overlays is null || nativeMap.Overlays.Length == 0),
					timeout: 5000,
					interval: 100,
					message: "Timed out waiting for the native overlays to clear.");
				await AssertEventually(
					() => InvokeOnMainThreadAsync(() => first.MapElementId is null),
					timeout: 5000,
					interval: 100,
					message: "Timed out waiting for the first polyline's native identity to clear.");

				var finalState = await InvokeOnMainThreadAsync(() => (
					SecondId: second.MapElementId,
					NativeOverlayCount: nativeMap.Overlays?.Length ?? 0));
				Assert.True(
					finalState.SecondId is null,
					$"Second polyline retained stale MapElementId after MapElements.Clear: actual={finalState.SecondId ?? "null"}; type={finalState.SecondId?.GetType().FullName ?? "null"}; expected=null; nativeOverlayCount={finalState.NativeOverlayCount}.");
			});
		}
	}
}
#endif

