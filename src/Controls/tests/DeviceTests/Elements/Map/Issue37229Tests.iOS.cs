using System;
using System.Linq;
using System.Threading.Tasks;
using MapKit;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Maps.Platform;
using Xunit;
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
					handlers.AddMauiControlsHandlers();
					handlers.AddMauiMaps();
					handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
				});
			});

			var map = new Map(MapSpan.FromCenterAndRadius(
				new Location(47.615, -122.335),
				Distance.FromKilometers(3)));

			var firstPolyline = new Polyline
			{
				StrokeColor = Colors.Red,
				StrokeWidth = 4,
				Geopath =
				{
					new Location(47.60, -122.33),
					new Location(47.61, -122.33)
				}
			};

			var secondPolyline = new Polyline
			{
				StrokeColor = Colors.Blue,
				StrokeWidth = 4,
				Geopath =
				{
					new Location(47.62, -122.34),
					new Location(47.63, -122.34)
				}
			};

			map.MapElements.Add(firstPolyline);
			map.MapElements.Add(secondPolyline);

			var mapHost = new ContentView { Content = map };
			var grid = new Grid
			{
				Padding = 20,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};

			grid.Children.Add(new Label
			{
				Text = "Issue 37229: MapElement tracking after a Geopath update",
				FontSize = 18
			});
			Grid.SetRow(mapHost, 1);
			grid.Children.Add(mapHost);

			var readyLabel = new Label { Text = "Both polylines have native IDs" };
			Grid.SetRow(readyLabel, 2);
			grid.Children.Add(readyLabel);

			var actions = new VerticalStackLayout { Spacing = 8 };
			actions.Children.Add(new Button { Text = "Update red polyline and clear map" });
			actions.Children.Add(new Label
			{
				Text = "Map element ID result",
				FontAttributes = FontAttributes.Bold
			});
			Grid.SetRow(actions, 3);
			grid.Children.Add(actions);

			var page = new ContentPage { Content = grid };

			await AttachAndRun(page, async _ =>
			{
				var mapHandler = Assert.IsType<MapHandler>(map.Handler);
				MauiMKMapView platformMap = mapHandler.PlatformView;
				Assert.NotNull(platformMap);

				await AssertEventually(
					() => firstPolyline.MapElementId is IMKOverlay &&
						secondPolyline.MapElementId is IMKOverlay &&
						platformMap.Overlays?.Length == 2,
					timeout: 5000,
					message: "Both polylines did not receive native overlays.");

				Assert.Equal(2, map.MapElements.Count);
				Assert.Same(firstPolyline, map.MapElements[0]);
				Assert.Same(secondPolyline, map.MapElements[1]);
				Assert.Equal(Colors.Red, firstPolyline.StrokeColor);
				Assert.Equal(Colors.Blue, secondPolyline.StrokeColor);
				Assert.Equal(4f, firstPolyline.StrokeWidth);
				Assert.Equal(4f, secondPolyline.StrokeWidth);
				Assert.Equal(2, firstPolyline.Geopath.Count);
				Assert.Equal(2, secondPolyline.Geopath.Count);

				var initialFirstId = Assert.IsAssignableFrom<IMKOverlay>(firstPolyline.MapElementId);
				var initialSecondId = Assert.IsAssignableFrom<IMKOverlay>(secondPolyline.MapElementId);
				Assert.NotSame(initialFirstId, initialSecondId);
				Assert.Contains(platformMap.Overlays, overlay => ReferenceEquals(overlay, initialFirstId));
				Assert.Contains(platformMap.Overlays, overlay => ReferenceEquals(overlay, initialSecondId));

				var updatedIdSentinel = new object();
				object updatedFirstId = updatedIdSentinel;
				firstPolyline.Geopath.Add(new Location(47.62, -122.33));

				await AssertEventually(
					() =>
					{
						var currentId = firstPolyline.MapElementId;
						if (currentId is not IMKOverlay ||
							ReferenceEquals(currentId, initialFirstId) ||
							platformMap.Overlays?.Any(overlay => ReferenceEquals(overlay, currentId)) != true)
						{
							return false;
						}

						updatedFirstId = currentId;
						return platformMap.Overlays.Any(overlay => ReferenceEquals(overlay, initialSecondId));
					},
					timeout: 5000,
					message: "The red polyline did not receive a replacement native overlay.");

				Assert.NotSame(updatedIdSentinel, updatedFirstId);
				Assert.NotSame(initialFirstId, updatedFirstId);
				Assert.Equal(3, firstPolyline.Geopath.Count);
				Assert.Same(secondPolyline, map.MapElements[1]);

				map.MapElements.Clear();

				await AssertEventually(
					() => map.MapElements.Count == 0,
					timeout: 5000,
					message: "MapElements did not clear.");
				await AssertEventually(
					() => platformMap.Overlays?.Length == 0,
					timeout: 5000,
					message: "Native map overlays did not clear.");
				await AssertEventually(
					() => firstPolyline.MapElementId is null,
					timeout: 5000,
					message: "The updated red polyline retained its native ID.");

				var secondId = secondPolyline.MapElementId;
				Assert.True(
					secondId is null,
					$"Issue 37229: second MapElementId should be null after update-and-clear. Expected: null; Actual type: {secondId?.GetType().FullName ?? "null"}; Actual value: {secondId ?? "null"}");
			});
		}
	}
#endif
}

