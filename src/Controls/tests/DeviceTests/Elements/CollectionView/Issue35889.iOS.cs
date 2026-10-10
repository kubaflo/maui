#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	[Category("Issue35889")]
	public class Issue35889 : ControlsHandlerTestBase
	{
		const double HeightTolerance = 0.5;

		[Fact]
		public async Task EmptyCollectionViewInAutoRowHasZeroHeight()
		{
			if (!System.OperatingSystem.IsIOSVersionAtLeast(26))
				return;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler2>();
				});
			});

			var (referencePage, referenceCollection, _, _) = CreateScenario();
			Assert.Null(referenceCollection.ItemsSource);
			referenceCollection.HeightRequest = 0;

			await CreateHandlerAndAddToWindow(referencePage, async () =>
			{
				var referenceHandler = Assert.IsType<CollectionViewHandler2>(referenceCollection.Handler);
				var referenceNativeView = referenceHandler.Controller.CollectionView;

				await AssertEventually(
					() => referenceNativeView.Superview is not null && referenceNativeView.Frame.Width > 0,
					message: "The reference UICollectionView was not attached with a positive width.");
				await WaitForStableFrames(referenceNativeView);

				Assert.InRange(referenceNativeView.Frame.Height, 0, HeightTolerance);
			});

			var (page, collection, beforeLabel, afterLabel) = CreateScenario();
			Assert.Null(collection.ItemsSource);
			int loadedState = -1;
			page.Loaded += (_, _) => loadedState = 1;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(() => loadedState == 1, message: "The reported page did not load.");
				Assert.Equal(1, loadedState);

				var collectionHandler = Assert.IsType<CollectionViewHandler2>(collection.Handler);
				var nativeCollection = collectionHandler.Controller.CollectionView;
				var nativeBeforeLabel = Assert.IsAssignableFrom<UILabel>(Assert.IsType<LabelHandler>(beforeLabel.Handler).PlatformView);
				var nativeAfterLabel = Assert.IsAssignableFrom<UILabel>(Assert.IsType<LabelHandler>(afterLabel.Handler).PlatformView);

				await AssertEventually(
					() => nativeCollection.Superview is not null && nativeCollection.Frame.Width > 0,
					message: "The target UICollectionView was not attached with a positive width.");
				await AssertEventually(
					() => nativeBeforeLabel.Superview is not null && nativeBeforeLabel.Frame.Width > 0,
					message: "The before label was not attached with a positive width.");
				await AssertEventually(
					() => nativeAfterLabel.Superview is not null && nativeAfterLabel.Frame.Width > 0,
					message: "The after label was not attached with a positive width.");

				await WaitForStableFrames(nativeCollection, nativeBeforeLabel, nativeAfterLabel);

				Assert.Equal("before collectionview", nativeBeforeLabel.Text);
				Assert.Equal("after collectionview", nativeAfterLabel.Text);
				var beforeFrame = GetWindowFrame(nativeBeforeLabel);
				var collectionFrame = GetWindowFrame(nativeCollection);
				var afterFrame = GetWindowFrame(nativeAfterLabel);
				Assert.True(beforeFrame.Y + beforeFrame.Height <= collectionFrame.Y + HeightTolerance);
				Assert.True(collectionFrame.Y + collectionFrame.Height <= afterFrame.Y + HeightTolerance);

				double height = nativeCollection.Frame.Height;
				Assert.True(
					Math.Abs(height) <= HeightTolerance,
					$"Issue35889: empty CollectionView native height must be 0; observed {height}, expected 0 +/- {HeightTolerance}");
			});
		}

		static (ContentPage Page, CollectionView Collection, Label BeforeLabel, Label AfterLabel) CreateScenario()
		{
			var beforeLabel = new Label { Text = "before collectionview" };
			var collection = new CollectionView
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
			grid.Add(collection, 0, 1);
			grid.Add(afterLabel, 0, 2);

			return (new ContentPage { Content = grid }, collection, beforeLabel, afterLabel);
		}

		static async Task WaitForStableFrames(params UIView[] views)
		{
			var previousFrames = new CoreGraphics.CGRect[views.Length];
			int stableSamples = 0;

			await AssertEventually(
				() =>
				{
					bool unchanged = true;
					for (int i = 0; i < views.Length; i++)
					{
						var currentFrame = GetWindowFrame(views[i]);
						unchanged &= currentFrame.Equals(previousFrames[i]);
						previousFrames[i] = currentFrame;
					}

					stableSamples = unchanged ? stableSamples + 1 : 0;
					return stableSamples >= 2;
				},
				timeout: 3000,
				message: "Native frames did not stabilize.");
		}

		static CoreGraphics.CGRect GetWindowFrame(UIView view) =>
			view.ConvertRectToView(view.Bounds, null);
	}
}
#endif

