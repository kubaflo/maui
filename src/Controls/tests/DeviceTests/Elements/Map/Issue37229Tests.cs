#if MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
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
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37229 : ControlsHandlerTestBase
	{
		void SetupBuilder()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Map, MapHandler>();
				});
			});
		}

		[Fact]
		public async Task ClearingAfterUpdatingOnePolylineClearsEveryMapElementId()
		{
			SetupBuilder();

			var first = new Polyline
			{
				StrokeColor = Colors.Red,
				StrokeWidth = 6,
				Geopath =
				{
					new Location(47.6000, -122.3300),
					new Location(47.6100, -122.3300)
				}
			};
			var second = new Polyline
			{
				StrokeColor = Colors.Blue,
				StrokeWidth = 6,
				Geopath =
				{
					new Location(47.6200, -122.3400),
					new Location(47.6300, -122.3400)
				}
			};
			var map = new Map(
				MapSpan.FromCenterAndRadius(
					new Location(47.6150, -122.3350),
					Distance.FromKilometers(3)));
			map.MapElements.Add(first);
			map.MapElements.Add(second);

			var mapHost = new ContentView
			{
				Content = map
			};
			var grid = new Grid
			{
				Padding = 16,
				RowSpacing = 10,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(new GridLength(320)),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				},
				Children = { mapHost }
			};
			Grid.SetRow(mapHost, 1);
			var page = new ContentPage
			{
				Content = grid
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var mapHandler = Assert.IsType<MapHandler>(map.Handler);
				MauiMKMapView platformView = mapHandler.PlatformView;

				await AssertEventually(
					() => platformView.Overlays?.Length == 2 &&
						first.MapElementId is not null &&
						second.MapElementId is not null,
					timeout: 5000,
					interval: 100,
					message: "Timed out waiting for both native map overlays.");

				Assert.Equal(2, platformView.Overlays.Length);
				Assert.NotNull(first.MapElementId);
				Assert.NotNull(second.MapElementId);
				var initialFirstId = first.MapElementId;
				var initialSecondId = second.MapElementId;
				Assert.NotSame(initialFirstId, initialSecondId);

				object updatedFirstId = null;
				first.Geopath.Add(new Location(47.6200, -122.3300));

				await AssertEventually(
					() =>
					{
						updatedFirstId = first.MapElementId;
						return updatedFirstId is not null &&
							!ReferenceEquals(initialFirstId, updatedFirstId) &&
							ReferenceEquals(initialSecondId, second.MapElementId) &&
							platformView.Overlays?.Length == 2;
					},
					timeout: 5000,
					interval: 100,
					message: "Timed out waiting for the first polyline native overlay to be replaced.");

				Assert.NotNull(updatedFirstId);
				Assert.NotSame(initialFirstId, updatedFirstId);
				Assert.Same(initialSecondId, second.MapElementId);
				Assert.Equal(2, platformView.Overlays.Length);

				int postClearOverlayCount = -1;
				map.MapElements.Clear();

				await AssertEventually(
					() =>
					{
						postClearOverlayCount = platformView.Overlays?.Length ?? 0;
						return postClearOverlayCount == 0;
					},
					timeout: 5000,
					interval: 100,
					message: "Timed out waiting for native map overlays to clear.");

				Assert.Equal(0, postClearOverlayCount);
				Assert.Empty(map.MapElements);
				Assert.True(
					first.MapElementId is null,
					$"First polyline retained stale MapElementId after clear; observed {(first.MapElementId is null ? "null" : "non-null")}.");
				Assert.True(
					second.MapElementId is null,
					$"Second polyline retained stale MapElementId after clear; first was {(first.MapElementId is null ? "null" : "non-null")} and second was {(second.MapElementId is null ? "null" : "non-null")}.");
			});
		}
	}
}
#endif

