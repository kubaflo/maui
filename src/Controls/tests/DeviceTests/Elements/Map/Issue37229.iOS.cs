using System.Threading.Tasks;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	public class Issue37229 : ControlsHandlerTestBase
	{
		[Fact]
		[Category(TestCategory.Map)]
		[Category("Issue37229")]
		public async Task ClearResetsEveryMapElementIdAfterUpdatingOneElement()
		{
			var (map, first, second) = await InvokeOnMainThreadAsync(() =>
			{
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

				return (map, first, second);
			});

			var handler = await CreateHandlerAsync<Microsoft.Maui.Maps.Handlers.MapHandler>(map);
			var platformView = handler.PlatformView;
			Assert.NotNull(platformView);

			await AssertEventually(
				() => InvokeOnMainThreadAsync(() =>
					first.MapElementId is not null &&
					second.MapElementId is not null &&
					platformView.Overlays?.Length == 2),
				message: "Map elements were not assigned their initial native overlays.");

			var initialFirstId = first.MapElementId;
			var initialSecondId = second.MapElementId;
			Assert.NotNull(initialFirstId);
			Assert.NotNull(initialSecondId);
			Assert.Equal(2, await InvokeOnMainThreadAsync(() => platformView.Overlays?.Length));

			const string notObserved = "not-observed";
			var updateObservation = notObserved;

			await InvokeOnMainThreadAsync(() =>
				first.Geopath.Add(new Location(47.62, -122.33)));

			await AssertEventually(
				() => InvokeOnMainThreadAsync(() =>
				{
					if (first.MapElementId is null || ReferenceEquals(first.MapElementId, initialFirstId))
						return false;

					updateObservation = "observed";
					return true;
				}),
				message: "The first Polyline did not receive a replacement native overlay.");

			Assert.NotEqual(notObserved, updateObservation);
			Assert.Same(second, map.MapElements[1]);
			Assert.Same(initialSecondId, second.MapElementId);
			Assert.Equal(2, await InvokeOnMainThreadAsync(() => platformView.Overlays?.Length));

			await InvokeOnMainThreadAsync(() => map.MapElements.Clear());

			await AssertEventually(
				() => InvokeOnMainThreadAsync(() =>
					map.MapElements.Count == 0 &&
					(platformView.Overlays is null || platformView.Overlays.Length == 0)),
				message: "The managed map elements and native overlays were not cleared.");

			Assert.Empty(map.MapElements);
			Assert.True(await InvokeOnMainThreadAsync(() =>
				platformView.Overlays is null || platformView.Overlays.Length == 0));
			Assert.True(
				first.MapElementId is null && second.MapElementId is null,
				$"After Geopath update and clear, expected both MapElementIds null; observed first={first.MapElementId ?? "<null>"}, second={second.MapElementId ?? "<null>"}.");
		}
	}
#endif
}

