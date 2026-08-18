#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	public class Issue35889 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionViewInAutoRowHasZeroNativeHeight()
		{
			Assert.True(OperatingSystem.IsIOSVersionAtLeast(26), "Issue35889 requires iOS 26 or later.");

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
				});
			});

			var beforeLabel = new Label { Text = "before collectionview" };
			var collectionView = new CollectionView
			{
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Colors.Red,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
				ItemTemplate = new DataTemplate(() => new Label { Text = "Hello World" })
			};
			var afterLabel = new Label { Text = "after collectionview" };
			var grid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(beforeLabel);
			grid.Add(collectionView, 0, 1);
			grid.Add(afterLabel, 0, 2);

			var page = new ContentPage { Content = grid };
			var loaded = false;
			var capturedNativeHeight = -1d;
			page.Loaded += (_, _) => loaded = true;

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				Assert.True(loaded, "The ContentPage.Loaded event must fire before native geometry is inspected.");

				var collectionHandler = Assert.IsType<CollectionViewHandler2>(collectionView.Handler);
				var nativeCollection = collectionHandler.Controller.CollectionView;
				var nativeBeforeLabel = Assert.IsAssignableFrom<UILabel>(beforeLabel.Handler.PlatformView);
				var nativeAfterLabel = Assert.IsAssignableFrom<UILabel>(afterLabel.Handler.PlatformView);

				await AssertEventually(
					() => nativeCollection.Window is not null
						&& nativeCollection.Window.Bounds.Width > 0
						&& nativeBeforeLabel.Window is not null
						&& nativeAfterLabel.Window is not null
						&& nativeBeforeLabel.Bounds.Width > 0
						&& nativeBeforeLabel.Bounds.Height > 0
						&& nativeAfterLabel.Bounds.Width > 0
						&& nativeAfterLabel.Bounds.Height > 0,
					message: "The CollectionView and both labels must be laid out in a native window.");

				var nativeWindow = nativeCollection.Window;
				Assert.True(nativeWindow.Bounds.Height > nativeWindow.Bounds.Width, "Issue35889 requires portrait window geometry.");
				Assert.Equal(UIUserInterfaceStyle.Light, nativeWindow.TraitCollection.UserInterfaceStyle);
				Assert.Equal(UIContentSizeCategory.Large.GetConstant(), nativeWindow.TraitCollection.PreferredContentSizeCategory);
				Assert.False(UIAccessibility.IsVoiceOverRunning, "Issue35889 requires default accessibility state.");

				Assert.Equal(3, grid.RowDefinitions.Count);
				Assert.All(grid.RowDefinitions, row => Assert.True(row.Height.IsAuto));
				Assert.Null(collectionView.ItemsSource);
				var linearLayout = Assert.IsType<LinearItemsLayout>(collectionView.ItemsLayout);
				Assert.Equal(ItemsLayoutOrientation.Vertical, linearLayout.Orientation);
				Assert.IsType<DataTemplate>(collectionView.ItemTemplate);
				Assert.Equal((nint)0, nativeCollection.NumberOfSections());
				Assert.Equal(0, collectionHandler.Controller.ItemsSource.ItemCount);
				Assert.Equal(Colors.Red, collectionView.BackgroundColor);
				Assert.Equal("before collectionview", nativeBeforeLabel.Text);
				Assert.Equal("after collectionview", nativeAfterLabel.Text);

				var collectionFrame = nativeCollection.ConvertRectToView(nativeCollection.Bounds, nativeWindow);
				var beforeFrame = nativeBeforeLabel.ConvertRectToView(nativeBeforeLabel.Bounds, nativeWindow);
				var afterFrame = nativeAfterLabel.ConvertRectToView(nativeAfterLabel.Bounds, nativeWindow);
				Assert.True(beforeFrame.Top < afterFrame.Top,
					$"Expected the before label above the after label; before={beforeFrame}, collection={collectionFrame}, after={afterFrame}.");

				capturedNativeHeight = collectionFrame.Height;
				Assert.True(Math.Abs(capturedNativeHeight) <= 1,
					$"Empty iOS CollectionView native height must be 0 (+/-1 pt); observed {capturedNativeHeight:F2}. Before={beforeFrame}, collection={collectionFrame}, after={afterFrame}.");
				Assert.True(Math.Abs(afterFrame.Top - beforeFrame.Bottom) <= 1,
					$"After-label top must equal before-label bottom (+/-1 pt); before bottom={beforeFrame.Bottom:F2}, after top={afterFrame.Top:F2}, collection={collectionFrame}.");
			});
		}
	}
}
#endif
