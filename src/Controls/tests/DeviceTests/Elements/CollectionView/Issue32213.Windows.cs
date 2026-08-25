#if WINDOWS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WListView = Microsoft.UI.Xaml.Controls.ListView;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WVisibility = Microsoft.UI.Xaml.Visibility;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue32213")]
	public class Issue32213 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HeaderAndFooterValuesUseTheirTemplates()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			int collectionLoadedState = -1;
			int collectionSizedState = -1;
			int headerFactoryCount = -1;
			int headerLoadedState = -1;
			int footerFactoryCount = -1;
			int footerLoadedState = -1;
			var collectionLoaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var collectionSized = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var itemLabels = new List<Label>();
			Label headerLabel = null;
			Label footerLabel = null;

			var itemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, new Binding("."));
				itemLabels.Add(label);
				return label;
			});
			var headerTemplate = new DataTemplate(() =>
			{
				headerFactoryCount = headerFactoryCount < 0 ? 1 : headerFactoryCount + 1;
				headerLabel = new Label { Text = "HeaderTemplate" };
				headerLabel.Loaded += (_, _) => headerLoadedState = 1;
				return headerLabel;
			});
			var footerTemplate = new DataTemplate(() =>
			{
				footerFactoryCount = footerFactoryCount < 0 ? 1 : footerFactoryCount + 1;
				footerLabel = new Label { Text = "FooterTemplate" };
				footerLabel.Loaded += (_, _) => footerLoadedState = 1;
				return footerLabel;
			});
			var items = new[] { "1", "2", "3", "4" };
			var collectionView = new CollectionView
			{
				SelectionMode = SelectionMode.Single,
				Header = "Header",
				Footer = "Footer",
				ItemsSource = items,
				ItemTemplate = itemTemplate,
				HeaderTemplate = headerTemplate,
				FooterTemplate = footerTemplate
			};
			collectionView.Loaded += (_, _) =>
			{
				collectionLoadedState = 1;
				collectionLoaded.TrySetResult(true);
			};
			collectionView.SizeChanged += (_, _) =>
			{
				if (collectionView.Width > 0.5 && collectionView.Height > 0.5)
				{
					collectionSizedState = 1;
					collectionSized.TrySetResult(true);
				}
			};

			var titleLabel = new Label
			{
				Text = "CollectionView HeaderTemplate and FooterTemplate on Windows",
				FontSize = 20
			};
			var resultLabel = new Label { Text = "Template rendering state" };
			var checkButton = new Button { Text = "Check rendered templates" };
			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star }
				}
			};
			Grid.SetRow(titleLabel, 0);
			Grid.SetRow(resultLabel, 1);
			Grid.SetRow(checkButton, 2);
			Grid.SetRow(collectionView, 3);
			grid.Children.Add(titleLabel);
			grid.Children.Add(resultLabel);
			grid.Children.Add(checkButton);
			grid.Children.Add(collectionView);

			var page = new ContentPage { Content = grid };
			var window = new Window(page);

			await CreateHandlerAndAddToWindow(window, async () =>
			{
				await Task.WhenAll(collectionLoaded.Task, collectionSized.Task).WaitAsync(TimeSpan.FromSeconds(5));

				Assert.Equal(1, collectionLoadedState);
				Assert.Equal(1, collectionSizedState);
				Assert.Same(items, collectionView.ItemsSource);
				Assert.Same(itemTemplate, collectionView.ItemTemplate);
				Assert.Same(headerTemplate, collectionView.HeaderTemplate);
				Assert.Same(footerTemplate, collectionView.FooterTemplate);
				Assert.Equal("Header", collectionView.Header);
				Assert.Equal("Footer", collectionView.Footer);
				Assert.Equal(SelectionMode.Single, collectionView.SelectionMode);

				var collectionHandler = Assert.IsAssignableFrom<CollectionViewHandler>(collectionView.Handler);
				var listView = Assert.IsAssignableFrom<WListView>(collectionHandler.PlatformView);

				await AssertEventually(
					() => itemLabels.Any(label => label.Text == "1" && label.Handler != null),
					timeout: 5000,
					message: "The first CollectionView item was not realized.");

				var firstItemLabel = itemLabels.First(label => label.Text == "1");
				var firstItemTextBlock = Assert.IsType<WTextBlock>(firstItemLabel.Handler.PlatformView);
				var nativeTextBlocks = listView.GetChildren<WTextBlock>().ToList();
				Assert.Equal(1, nativeTextBlocks.Count(element => ReferenceEquals(element, firstItemTextBlock)));
				Assert.True(firstItemTextBlock.IsLoaded);
				Assert.Equal(WVisibility.Visible, firstItemTextBlock.Visibility);
				Assert.True(firstItemTextBlock.ActualWidth > 0.5);
				Assert.True(firstItemTextBlock.ActualHeight > 0.5);

				bool templatesRealized = await Wait(
					() => headerLoadedState == 1
						&& footerLoadedState == 1
						&& headerLabel != null
						&& footerLabel != null
						&& headerLabel.Handler != null
						&& footerLabel.Handler != null,
					timeout: 5000);

				nativeTextBlocks = listView.GetChildren<WTextBlock>().ToList();
				var headerTextBlock = headerLabel != null && headerLabel.Handler != null
					? headerLabel.Handler.PlatformView as WTextBlock
					: null;
				var footerTextBlock = footerLabel != null && footerLabel.Handler != null
					? footerLabel.Handler.PlatformView as WTextBlock
					: null;
				int headerDescendantCount = headerTextBlock == null
					? 0
					: nativeTextBlocks.Count(element => ReferenceEquals(element, headerTextBlock));
				int footerDescendantCount = footerTextBlock == null
					? 0
					: nativeTextBlocks.Count(element => ReferenceEquals(element, footerTextBlock));
				int headerTextCount = nativeTextBlocks.Count(element => element.Text == "HeaderTemplate");
				int footerTextCount = nativeTextBlocks.Count(element => element.Text == "FooterTemplate");
				bool headerVisible = headerTextBlock != null
					&& headerTextBlock.Text == "HeaderTemplate"
					&& headerLoadedState == 1
					&& headerTextBlock.IsLoaded
					&& headerTextBlock.Visibility == WVisibility.Visible
					&& headerTextBlock.ActualWidth > 0.5
					&& headerTextBlock.ActualHeight > 0.5
					&& headerDescendantCount == 1
					&& headerTextCount == 1;
				bool footerVisible = footerTextBlock != null
					&& footerTextBlock.Text == "FooterTemplate"
					&& footerLoadedState == 1
					&& footerTextBlock.IsLoaded
					&& footerTextBlock.Visibility == WVisibility.Visible
					&& footerTextBlock.ActualWidth > 0.5
					&& footerTextBlock.ActualHeight > 0.5
					&& footerDescendantCount == 1
					&& footerTextCount == 1;

				Assert.True(
					templatesRealized && headerVisible && footerVisible,
					$"CollectionView template visibility mismatch: header factory={headerFactoryCount}, loaded={headerLoadedState}, visible={headerTextBlock?.Visibility}, descendants={headerDescendantCount}, textCount={headerTextCount}, size={headerTextBlock?.ActualWidth}x{headerTextBlock?.ActualHeight}; footer factory={footerFactoryCount}, loaded={footerLoadedState}, visible={footerTextBlock?.Visibility}, descendants={footerDescendantCount}, textCount={footerTextCount}, size={footerTextBlock?.ActualWidth}x{footerTextBlock?.ActualHeight}");
			});
		}
	}
}
#endif

