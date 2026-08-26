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
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
#if IOS && !MACCATALYST
	[Category("Issue37229")]
	public class Issue37229 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ClearingAfterUpdatingOnePolylineResetsEveryMapElementId()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.UseMauiMaps();
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<ContentView, ContentViewHandler>();
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

			var map = new Map
			{
				AutomationId = "IssueMap"
			};
			map.MapElements.Add(first);
			map.MapElements.Add(second);

			var triggerButton = new Button
			{
				AutomationId = "TriggerButton",
				Text = "Mutate first polyline and clear"
			};
			var mapHost = new ContentView
			{
				AutomationId = "MapHost",
				MinimumHeightRequest = 280,
				Content = map
			};
			var grid = new Grid
			{
				Padding = 16,
				RowSpacing = 10,
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
				Text = "Two tracked polylines before collection clear"
			}, 0, 0);
			grid.Add(mapHost, 0, 1);
			grid.Add(new Label
			{
				AutomationId = "TrackingState",
				Text = "Map element tracking state"
			}, 0, 2);
			grid.Add(triggerButton, 0, 3);
			grid.Add(new Label
			{
				AutomationId = "ResultLabel",
				FontAttributes = FontAttributes.Bold,
				Text = "Map element clear result"
			}, 0, 4);

			var page = new ContentPage
			{
				Title = "Issue 37229",
				Content = grid
			};

			object firstIdAfterUpdate = null;
			object secondIdAfterUpdate = null;
			int clickedCallback = -1;

			triggerButton.Clicked += (_, _) =>
			{
				first.Geopath.Add(new Location(47.62, -122.33));
				firstIdAfterUpdate = first.MapElementId;
				secondIdAfterUpdate = second.MapElementId;
				clickedCallback = 0;
				map.MapElements.Clear();
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(map.Handler);
				Assert.NotNull(triggerButton.Handler);

				var mapHandler = Assert.IsType<MapHandler>(map.Handler);
				var platformMap = Assert.IsType<MauiMKMapView>(mapHandler.PlatformView);
				var buttonHandler = Assert.IsType<ButtonHandler>(triggerButton.Handler);
				var platformButton = Assert.IsType<UIButton>(buttonHandler.PlatformView);

				await InvokeOnMainThreadAsync(() =>
					map.MoveToRegion(MapSpan.FromCenterAndRadius(
						new Location(47.615, -122.335),
						Distance.FromKilometers(5))));

				bool bothIdsCreated = await AssertHelpers.Wait(
					() => first.MapElementId is not null && second.MapElementId is not null,
					timeout: 5000);
				Assert.True(bothIdsCreated, "Both Polyline MapElementId values should be created after attachment.");

				bool bothOverlaysCreated = await AssertHelpers.Wait(
					() => platformMap.Overlays?.Length == 2,
					timeout: 5000);
				Assert.True(bothOverlaysCreated, "The native map should contain exactly two overlays after attachment.");

				object initialFirstId = first.MapElementId;
				object initialSecondId = second.MapElementId;

				await InvokeOnMainThreadAsync(() =>
					platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside));

				Assert.NotEqual(-1, clickedCallback);
				Assert.NotNull(firstIdAfterUpdate);
				Assert.NotSame(initialFirstId, firstIdAfterUpdate);
				Assert.Same(initialSecondId, secondIdAfterUpdate);

				bool managedCollectionCleared = await AssertHelpers.Wait(
					() => map.MapElements.Count == 0,
					timeout: 5000);
				Assert.True(managedCollectionCleared, "The managed MapElements collection should be empty after the trigger.");

				bool nativeOverlaysCleared = await AssertHelpers.Wait(
					() => platformMap.Overlays is null || platformMap.Overlays.Length == 0,
					timeout: 5000);
				Assert.True(nativeOverlaysCleared, "The native map should contain no overlays after MapElements.Clear().");

				Assert.Null(first.MapElementId);
				Assert.True(
					second.MapElementId is null,
					$"Issue37229: second Polyline MapElementId remained stale after first Geopath update and MapElements.Clear(). Observed: {second.MapElementId}; expected: null.");
			});
		}
	}
#endif
}

