#if MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
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
		public async Task ClearingAfterUpdatingOnePolylineResetsEveryMapElementId()
		{
			EnsureHandlerCreated(builder =>
				builder.ConfigureMauiHandlers(handlers => handlers.AddMauiControlsHandlers()));

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

			var map = new Map
			{
				MapElements =
				{
					first,
					second
				}
			};

			var grid = new Grid
			{
				Padding = 16,
				RowSpacing = 10,
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				}
			};

			grid.Add(new Label
			{
				Text = "Issue 37229: mutate one polyline, then clear both",
				FontAttributes = FontAttributes.Bold
			}, 0, 0);
			grid.Add(new Label { Text = "Map overlay identifiers" }, 0, 1);
			grid.Add(new Label { Text = "Polyline lifecycle state" }, 0, 2);
			grid.Add(new ContentView { Content = map }, 0, 3);
			grid.Add(new VerticalStackLayout
			{
				Spacing = 8,
				Children =
				{
					new Label { Text = "Map element result" },
					new Button { Text = "Mutate first polyline and clear" }
				}
			}, 0, 4);

			var page = new ContentPage { Content = grid };

			Assert.Same(first, map.MapElements[0]);
			Assert.Same(second, map.MapElements[1]);
			Assert.Equal((47.60, -122.33), (first.Geopath[0].Latitude, first.Geopath[0].Longitude));
			Assert.Equal((47.61, -122.33), (first.Geopath[1].Latitude, first.Geopath[1].Longitude));
			Assert.Equal((47.62, -122.34), (second.Geopath[0].Latitude, second.Geopath[0].Longitude));
			Assert.Equal((47.63, -122.34), (second.Geopath[1].Latitude, second.Geopath[1].Longitude));

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var handler = Assert.IsType<Microsoft.Maui.Maps.Handlers.MapHandler>(map.Handler);
				var platformView = Assert.IsType<MauiMKMapView>(handler.PlatformView);

				await AssertEventually(
					() => first.MapElementId is not null &&
						second.MapElementId is not null &&
						platformView.Overlays?.Length == 2,
					message: "Both polylines were not attached as native overlays.");

				var firstInitialId = first.MapElementId;
				var secondInitialId = second.MapElementId;
				Assert.NotSame(firstInitialId, secondInitialId);
				Assert.Contains(platformView.Overlays, overlay => ReferenceEquals(overlay, firstInitialId));
				Assert.Contains(platformView.Overlays, overlay => ReferenceEquals(overlay, secondInitialId));

				object firstUpdatedId = null;
				await InvokeOnMainThreadAsync(() =>
					first.Geopath.Add(new Location(47.62, -122.33)));

				await AssertEventually(() =>
				{
					firstUpdatedId = first.MapElementId;
					return firstUpdatedId is not null &&
						!ReferenceEquals(firstUpdatedId, firstInitialId);
				}, message: "Updating the first polyline did not replace its native identifier.");

				Assert.Same(secondInitialId, second.MapElementId);
				Assert.Contains(platformView.Overlays, overlay => ReferenceEquals(overlay, firstUpdatedId));
				Assert.Contains(platformView.Overlays, overlay => ReferenceEquals(overlay, secondInitialId));

				int postClearOverlayCount = -1;
				await InvokeOnMainThreadAsync(map.MapElements.Clear);
				await AssertEventually(() =>
				{
					postClearOverlayCount = platformView.Overlays?.Length ?? 0;
					return postClearOverlayCount == 0;
				}, message: "Native overlays were not removed after clearing MapElements.");

				Assert.Empty(map.MapElements);
				string identifierStates =
					$"first={(first.MapElementId is null ? "null" : "non-null")}, " +
					$"second={(second.MapElementId is null ? "null" : "non-null")}, " +
					$"overlays={postClearOverlayCount}";
				Assert.True(first.MapElementId is null,
					$"Updated polyline retained stale MapElementId after clear: {identifierStates}");
				Assert.True(second.MapElementId is null,
					$"Untouched polyline retained stale MapElementId after clear: {identifierStates}");
			});
		}
	}
}
#endif

