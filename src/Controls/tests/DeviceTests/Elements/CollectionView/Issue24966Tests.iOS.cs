using System;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if IOS && !MACCATALYST
	[Collection(RunInNewWindowCollection)]
	[Category("Issue24966")]
	public class Issue24966 : ControlsHandlerTestBase
	{
		const double HeightTolerance = 1;

		[Fact]
		public async Task EmptyViewUsesIntrinsicHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler2>();
				});
			});

			var cleanLabel = new Label
			{
				AutomationId = "Issue24966CleanLabel",
				Text = "The collection is empty"
			};
			var cleanGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto)
				}
			};
			cleanGrid.Add(cleanLabel);

			await CreateHandlerAndAddToWindow<IWindowHandler>(
				new ContentPage { Content = cleanGrid },
				async _ =>
				{
					await AssertEventually(() =>
						cleanLabel.Handler?.PlatformView is UILabel label &&
						label.Window is not null &&
						label.Bounds.Width > 0 &&
						label.Bounds.Height > 0);

					var cleanNativeLabel = Assert.IsAssignableFrom<UILabel>(cleanLabel.Handler.PlatformView);
					Assert.Equal("The collection is empty", cleanNativeLabel.Text);
					Assert.Equal("Issue24966CleanLabel", cleanNativeLabel.AccessibilityIdentifier);

					var cleanIntrinsicHeight = cleanNativeLabel
						.SizeThatFits(new CGSize(cleanNativeLabel.Bounds.Width, nfloat.MaxValue))
						.Height;

					Assert.True(
						Math.Abs(cleanNativeLabel.Bounds.Height - cleanIntrinsicHeight) <= HeightTolerance,
						$"Clean Label native height was {cleanNativeLabel.Bounds.Height:F1}; expected intrinsic height {cleanIntrinsicHeight:F1}.");
				});

			var emptyLabel = new Label
			{
				AutomationId = "Issue24966EmptyView",
				Text = "The collection is empty"
			};
			var footerLabel = new Label
			{
				AutomationId = "Issue24966Footer",
				Text = "Collection footer"
			};
			var collectionView = new CollectionView
			{
				AutomationId = "Issue24966CollectionView",
				Header = new Label
				{
					AutomationId = "Issue24966Header",
					Text = "Collection header"
				},
				EmptyView = emptyLabel,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label();
					itemLabel.SetBinding(Label.TextProperty, ".");
					return itemLabel;
				}),
				Footer = footerLabel,
				ItemsSource = Array.Empty<string>()
			};
			var pageGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			pageGrid.Add(collectionView, 0, 0);
			pageGrid.Add(new Button
			{
				AutomationId = "Issue24966Check",
				IsEnabled = false,
				Text = "Check empty view height"
			}, 0, 1);
			pageGrid.Add(new Label
			{
				AutomationId = "Issue24966Measurement",
				Text = "Waiting for intrinsic height"
			}, 0, 2);
			pageGrid.Add(new Label
			{
				AutomationId = "Issue24966LayoutNote",
				Text = "Collection layout"
			}, 0, 3);

			var page = new ContentPage
			{
				Title = "Issue 24966",
				Content = pageGrid
			};
			var loadedTransition = new TaskCompletionSource<int>();
			var loadedSentinel = -1;
			page.Loaded += (_, _) =>
			{
				loadedSentinel = 1;
				loadedTransition.TrySetResult(loadedSentinel);
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(
				page,
				async _ =>
				{
					await loadedTransition.Task.WaitAsync(TimeSpan.FromSeconds(2));
					Assert.Equal(1, loadedSentinel);
					await OnLoadedAsync(collectionView);
					await OnLoadedAsync(emptyLabel);
					await OnLoadedAsync(footerLabel);

					await AssertEventually(() =>
						collectionView.Handler is CollectionViewHandler2 collectionHandler &&
						collectionHandler.PlatformView is UIView nativeCollectionRoot &&
						nativeCollectionRoot.Window is not null &&
						nativeCollectionRoot.Bounds.Width > 0 &&
						nativeCollectionRoot.Bounds.Height > 0 &&
						collectionHandler.Controller.CollectionView is UICollectionView nativeCollection &&
						nativeCollection.Window is not null &&
						nativeCollection.Bounds.Width > 0 &&
						nativeCollection.Bounds.Height > 0 &&
						emptyLabel.Handler?.PlatformView is UILabel nativeEmpty &&
						nativeEmpty.Window is not null &&
						nativeEmpty.Bounds.Width > 0 &&
						nativeEmpty.Bounds.Height > 0 &&
						footerLabel.Handler?.PlatformView is UILabel nativeFooter &&
						nativeFooter.Window is not null &&
						nativeFooter.Bounds.Width > 0 &&
						nativeFooter.Bounds.Height > 0);

					var collectionHandler = Assert.IsType<CollectionViewHandler2>(collectionView.Handler);
					var collectionNativeRoot = Assert.IsAssignableFrom<UIView>(collectionHandler.PlatformView);
					var collectionNativeView = Assert.IsAssignableFrom<UICollectionView>(collectionHandler.Controller.CollectionView);
					var emptyNativeView = Assert.IsAssignableFrom<UILabel>(emptyLabel.Handler.PlatformView);
					var footerNativeView = Assert.IsAssignableFrom<UILabel>(footerLabel.Handler.PlatformView);
					var nativeWindow = Assert.IsAssignableFrom<UIWindow>(collectionNativeRoot.Window);

					Assert.Equal("Issue24966CollectionView", collectionNativeRoot.AccessibilityIdentifier);
					Assert.Same(nativeWindow, collectionNativeView.Window);
					Assert.Same(nativeWindow, emptyNativeView.Window);
					Assert.Same(nativeWindow, footerNativeView.Window);
					Assert.Equal("The collection is empty", emptyNativeView.Text);
					Assert.Equal("Issue24966EmptyView", emptyNativeView.AccessibilityIdentifier);
					Assert.Equal("Collection footer", footerNativeView.Text);
					Assert.Equal("Issue24966Footer", footerNativeView.AccessibilityIdentifier);

					var emptyWindowFrame = emptyNativeView.ConvertRectToView(emptyNativeView.Bounds, nativeWindow);
					var footerWindowFrame = footerNativeView.ConvertRectToView(footerNativeView.Bounds, nativeWindow);
					Assert.True(nativeWindow.Bounds.Width > 0 && nativeWindow.Bounds.Height > 0, "The native test surface must have nonzero bounds.");
					Assert.True(nativeWindow.Bounds.Contains(emptyWindowFrame), "The EmptyView must be within the native test surface.");
					Assert.True(nativeWindow.Bounds.Contains(footerWindowFrame), "The footer must be within the native test surface.");

					var expectedHeight = emptyNativeView
						.SizeThatFits(new CGSize(emptyNativeView.Bounds.Width, nfloat.MaxValue))
						.Height;
					var actualHeight = emptyNativeView.Bounds.Height;

					Assert.True(
						Math.Abs(actualHeight - expectedHeight) <= HeightTolerance,
						$"Issue24966 EmptyView native height was {actualHeight:F1}; expected intrinsic height {expectedHeight:F1}.");
				});
		}
	}
#endif
}

