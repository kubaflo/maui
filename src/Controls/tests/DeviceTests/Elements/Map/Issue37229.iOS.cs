#if MACCATALYST
using System.Collections.Specialized;
using System.Threading.Tasks;
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
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.Map)]
	[Category("Issue37229")]
	public class Issue37229 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ClearingAfterUpdatingOnePolylineResetsAllMapElementIds()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
				});
			});

			var map = new Map(
				MapSpan.FromCenterAndRadius(
					new Location(47.615, -122.335),
					Distance.FromKilometers(5)));

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

			map.MapElements.Add(first);
			map.MapElements.Add(second);

			var mapHost = new ContentView
			{
				Content = map
			};
			Grid.SetRow(mapHost, 1);

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
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(mapHost);

			var page = new ContentPage
			{
				Content = grid
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var handler = Assert.IsType<MapHandler>(map.Handler);
				MauiMKMapView platformView = handler.PlatformView;

				await AssertEventually(
					() => platformView.Frame.Width > 0 &&
						platformView.Frame.Height > 0 &&
						platformView.Overlays?.Length == 2 &&
						first.MapElementId is not null &&
						second.MapElementId is not null,
					timeout: 5000,
					interval: 100,
					message: "The map and both native polyline overlays were not ready.");

				Assert.NotSame(first.MapElementId, second.MapElementId);

				var originalFirstMapElementId = first.MapElementId;
				var observedGeopathCount = -1;
				NotifyCollectionChangedEventArgs observedReset = null;

				first.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == nameof(Polyline.Geopath))
						observedGeopathCount = first.Geopath.Count;
				};
				((INotifyCollectionChanged)map.MapElements).CollectionChanged += (_, args) =>
				{
					if (args.Action == NotifyCollectionChangedAction.Reset)
						observedReset = args;
				};

				first.Geopath.Add(new Location(47.62, -122.33));

				await AssertEventually(
					() => observedGeopathCount == 3 &&
						first.MapElementId is not null &&
						!ReferenceEquals(originalFirstMapElementId, first.MapElementId),
					timeout: 5000,
					interval: 100,
					message: "The red polyline update did not reach the native map.");

				map.MapElements.Clear();

				Assert.NotNull(observedReset);
				Assert.Equal(NotifyCollectionChangedAction.Reset, observedReset.Action);

				await AssertEventually(
					() => map.MapElements.Count == 0 && (platformView.Overlays?.Length ?? 0) == 0,
					timeout: 5000,
					interval: 100,
					message: "MapElements.Clear did not remove the managed and native overlays.");

				var firstReset = first.MapElementId is null;
				var secondReset = second.MapElementId is null;
				var remainingNativeOverlays = platformView.Overlays?.Length ?? 0;

				Assert.True(
					firstReset && secondReset,
					$"Issue37229: clearing MapElements after updating the red polyline must reset both MapElementIds; firstReset={firstReset}; secondReset={secondReset}; remainingNativeOverlays={remainingNativeOverlays}");
			});
		}
	}
}
#endif

