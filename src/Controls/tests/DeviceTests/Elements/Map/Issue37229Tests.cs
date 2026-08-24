#if MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Maps.Handlers;
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
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddMauiMaps();
				});
			});

			var map = new Map();
			var first = CreatePolyline(Colors.Red, 47.60, -122.33, 47.61, -122.33);
			var second = CreatePolyline(Colors.Blue, 47.62, -122.34, 47.63, -122.34);
			map.MapElements.Add(first);
			map.MapElements.Add(second);

			var mapHost = new ContentView { Content = map };
			var grid = new Grid
			{
				Padding = 20,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(new GridLength(320)),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(mapHost, 0, 1);
			var page = new ContentPage { Content = grid };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var handler = Assert.IsType<MapHandler>(map.Handler);
				var platformView = handler.PlatformView;
				Assert.NotNull(platformView);

				await AssertEventually(
					() => platformView.Overlays?.Length == 2 &&
						first.MapElementId is not null &&
						second.MapElementId is not null &&
						!ReferenceEquals(first.MapElementId, second.MapElementId),
					timeout: 5000,
					message: "Both polylines were not added as distinct native overlays.");

				var firstIdentifier = first.MapElementId;
				Assert.NotNull(firstIdentifier);
				var updateObserved = false;

				first.Geopath.Add(new Location(47.62, -122.33));
				Assert.Equal(3, first.Geopath.Count);

				await AssertEventually(
					() =>
					{
						updateObserved =
							first.MapElementId is not null &&
							!ReferenceEquals(firstIdentifier, first.MapElementId) &&
							platformView.Overlays?.Length == 2;
						return updateObserved;
					},
					timeout: 5000,
					message: "The first polyline was not replaced by the native map handler.");
				Assert.True(updateObserved, "The first polyline update was not observed.");

				map.MapElements.Clear();

				await AssertEventually(
					() => platformView.Overlays is null || platformView.Overlays.Length == 0,
					timeout: 5000,
					message: "Native overlays were not cleared.");

				Assert.Empty(map.MapElements);
				Assert.Null(first.MapElementId);
				Assert.True(
					second.MapElementId is null,
					$"Issue37229: untouched second polyline retained stale MapElementId after clear. Stale identifier: {second.MapElementId}; native overlay count: {platformView.Overlays?.Length ?? 0}.");
			});
		}

		static Polyline CreatePolyline(Color color, double startLatitude, double startLongitude, double endLatitude, double endLongitude)
		{
			var polyline = new Polyline
			{
				StrokeColor = color,
				StrokeWidth = 4
			};

			polyline.Geopath.Add(new Location(startLatitude, startLongitude));
			polyline.Geopath.Add(new Location(endLatitude, endLongitude));
			return polyline;
		}
	}
}
#endif

