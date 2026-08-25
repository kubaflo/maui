using System.Threading.Tasks;
using MapKit;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Category(TestCategory.Map, "Issue37229")]
	public class Issue37229 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ClearAfterUpdatingOnePolylineResetsAllMapElementIds()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<HorizontalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Map, MapHandler>();
				});
			});

			var map = new Map();
			var redPolyline = new Polyline
			{
				StrokeColor = Colors.Red,
				StrokeWidth = 4
			};
			redPolyline.Geopath.Add(new Location(47.60, -122.33));
			redPolyline.Geopath.Add(new Location(47.61, -122.33));

			var bluePolyline = new Polyline
			{
				StrokeColor = Colors.Blue,
				StrokeWidth = 4
			};
			bluePolyline.Geopath.Add(new Location(47.62, -122.34));
			bluePolyline.Geopath.Add(new Location(47.63, -122.34));

			map.MapElements.Add(redPolyline);
			map.MapElements.Add(bluePolyline);
			map.MoveToRegion(MapSpan.FromCenterAndRadius(
				new Location(47.615, -122.335),
				Distance.FromKilometers(5)));

			var mapHost = new Grid
			{
				MinimumHeightRequest = 320
			};
			mapHost.Children.Add(map);

			var readyLabel = new Label
			{
				AutomationId = "ReadyState",
				Text = "READY: Waiting for both map elements"
			};
			var elementStateLabel = new Label
			{
				AutomationId = "ElementState",
				Text = "ELEMENTS: Red=pending; Blue=pending"
			};
			var triggerButton = new Button
			{
				AutomationId = "TriggerUpdateAndClear",
				IsEnabled = false,
				Text = "Update red path and clear"
			};
			var expectationLabel = new Label
			{
				VerticalOptions = LayoutOptions.Center,
				Text = "All map element identifiers should be reset"
			};
			var actionRow = new HorizontalStackLayout
			{
				Spacing = 12,
				Children =
				{
					triggerButton,
					expectationLabel
				}
			};

			var rootGrid = new Grid
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
			var headingLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = "MapElement tracking after Geopath update"
			};
			rootGrid.Children.Add(headingLabel);
			rootGrid.Children.Add(mapHost);
			rootGrid.Children.Add(readyLabel);
			rootGrid.Children.Add(elementStateLabel);
			rootGrid.Children.Add(actionRow);
			Grid.SetRow(mapHost, 1);
			Grid.SetRow(readyLabel, 2);
			Grid.SetRow(elementStateLabel, 3);
			Grid.SetRow(actionRow, 4);

			var page = new ContentPage
			{
				Content = rootGrid
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(() =>
					map.Handler is MapHandler &&
					redPolyline.MapElementId is not null &&
					bluePolyline.MapElementId is not null);

				var mapHandler = map.Handler as MapHandler;
				Assert.NotNull(mapHandler);
				var platformMap = mapHandler.PlatformView;

				Assert.Equal(2, map.MapElements.Count);
				Assert.Same(redPolyline, map.MapElements[0]);
				Assert.Same(bluePolyline, map.MapElements[1]);
				Assert.Equal(2, redPolyline.Geopath.Count);
				Assert.Equal(47.60, redPolyline.Geopath[0].Latitude);
				Assert.Equal(-122.33, redPolyline.Geopath[0].Longitude);
				Assert.Equal(2, bluePolyline.Geopath.Count);
				Assert.Equal(47.62, bluePolyline.Geopath[0].Latitude);
				Assert.Equal(-122.34, bluePolyline.Geopath[0].Longitude);

				await AssertEventually(() =>
					platformMap.Overlays is not null &&
					platformMap.Overlays.Length == 2);

				var originalRedId = redPolyline.MapElementId;
				var originalBlueId = bluePolyline.MapElementId;
				Assert.NotNull(originalRedId);
				Assert.NotNull(originalBlueId);
				Assert.NotSame(originalRedId, originalBlueId);
				Assert.Contains(platformMap.Overlays, overlay => object.ReferenceEquals(overlay, originalRedId));
				Assert.Contains(platformMap.Overlays, overlay => object.ReferenceEquals(overlay, originalBlueId));

				readyLabel.Text = "READY: Both map element identifiers are set";
				elementStateLabel.Text = "ELEMENTS: Red=set; Blue=set";
				triggerButton.IsEnabled = true;

				bool clickedObserved = false;
				bool geopathNotificationObserved = false;
				bool redOverlayReplacementObserved = false;
				object replacementRedId = new object();

				redPolyline.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == nameof(Polyline.Geopath))
						geopathNotificationObserved = true;
				};
				triggerButton.Clicked += (_, _) =>
				{
					clickedObserved = true;
					redPolyline.Geopath.Add(new Location(47.62, -122.33));
					replacementRedId = redPolyline.MapElementId;
					if (replacementRedId is IMKOverlay && platformMap.Overlays is not null)
					{
						foreach (var overlay in platformMap.Overlays)
						{
							if (object.ReferenceEquals(overlay, replacementRedId))
								redOverlayReplacementObserved = true;
						}
					}
					map.MapElements.Clear();
					elementStateLabel.Text = $"ELEMENTS AFTER CLEAR: Red={(redPolyline.MapElementId is null ? "null" : "set")}; Blue={(bluePolyline.MapElementId is null ? "null" : "set")}";
				};

				var buttonHandler = triggerButton.Handler as ButtonHandler;
				Assert.NotNull(buttonHandler);
				buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await AssertEventually(() =>
					clickedObserved &&
					geopathNotificationObserved &&
					redOverlayReplacementObserved &&
					!object.ReferenceEquals(replacementRedId, originalRedId) &&
					map.MapElements.Count == 0 &&
					(platformMap.Overlays is null || platformMap.Overlays.Length == 0));

				Assert.NotNull(replacementRedId);
				Assert.Equal(3, redPolyline.Geopath.Count);
				Assert.Equal(47.62, redPolyline.Geopath[2].Latitude);
				Assert.Equal(-122.33, redPolyline.Geopath[2].Longitude);
				Assert.Empty(map.MapElements);
				Assert.True(platformMap.Overlays is null || platformMap.Overlays.Length == 0);
				Assert.True(
					redPolyline.MapElementId is null && bluePolyline.MapElementId is null,
					$"Clear after red Geopath update must reset both MapElementIds; red={redPolyline.MapElementId}; blue={bluePolyline.MapElementId}; overlays={platformMap.Overlays?.Length ?? 0}");
			});
		}
	}
#endif
}

