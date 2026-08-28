#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue24968")]
	public class Issue24968 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task TemplatesRemainVisibleAfterItemsSourceBecomesEmpty()
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

			Label headerLabel = null;
			Label itemLabel = null;
			Label emptyLabel = null;
			Label footerLabel = null;

			var collectionView = new CollectionView
			{
				HeaderTemplate = new DataTemplate(() =>
				{
					headerLabel = new Label { Text = "HEADER TEMPLATE" };
					return headerLabel;
				}),
				ItemTemplate = new DataTemplate(() =>
				{
					itemLabel = new Label();
					itemLabel.SetBinding(Label.TextProperty, ".");
					return itemLabel;
				}),
				EmptyViewTemplate = new DataTemplate(() =>
				{
					emptyLabel = new Label { Text = "EMPTY VIEW TEMPLATE" };
					return emptyLabel;
				}),
				FooterTemplate = new DataTemplate(() =>
				{
					footerLabel = new Label { Text = "FOOTER TEMPLATE" };
					return footerLabel;
				}),
				ItemsSource = new[] { "Initial item" }
			};

			var showEmptyViewButton = new Button
			{
				Text = "Show empty collection",
				Command = new Command(() => collectionView.ItemsSource = Array.Empty<string>())
			};

			var layout = new Grid
			{
				Padding = 20,
				RowDefinitions = new RowDefinitionCollection(
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)),
				RowSpacing = 12
			};

			layout.Add(new Label
			{
				Text = "CollectionView template visibility",
				FontSize = 22
			}, 0, 0);
			layout.Add(new Label
			{
				Text = "The header and footer should remain visible when the collection changes to its empty view."
			}, 0, 1);
			layout.Add(collectionView, 0, 2);
			layout.Add(showEmptyViewButton, 0, 3);

			var page = new ContentPage { Content = layout };

			await CreateHandlerAndAddToWindow<LayoutHandler>(page, async _ =>
			{
				var collectionViewHandler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var nativeCollectionView = Assert.IsAssignableFrom<WListViewBase>(collectionViewHandler.PlatformView);

				await AssertEventually(() =>
				{
					var headerState = GetTemplateState(headerLabel, "HEADER TEMPLATE");
					var itemState = GetTemplateState(itemLabel, "Initial item");
					var footerState = GetTemplateState(footerLabel, "FOOTER TEMPLATE");
					return nativeCollectionView.Items.Count == 1 &&
						headerState.IsVisible &&
						itemState.IsVisible &&
						footerState.IsVisible;
				}, timeout: 5000, message: "The populated CollectionView templates did not reach a visible native baseline.");

				bool itemsSourceChanged = false;
				collectionView.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == nameof(ItemsView.ItemsSource))
						itemsSourceChanged = true;
				};

				Assert.NotNull(showEmptyViewButton.Command);
				showEmptyViewButton.Command.Execute(null);

				await AssertEventually(
					() => itemsSourceChanged,
					message: "The Button command did not raise the post-trigger ItemsSource property notification.");
				await AssertEventually(
					() => nativeCollectionView.Items.Count == 0,
					message: "The native CollectionView did not reach the post-trigger zero-item state.");

				TemplateState headerState = default;
				TemplateState footerState = default;
				TemplateState emptyState = default;
				int observations = 0;

				await AssertEventually(() =>
				{
					headerState = GetTemplateState(headerLabel, "HEADER TEMPLATE");
					footerState = GetTemplateState(footerLabel, "FOOTER TEMPLATE");
					emptyState = GetTemplateState(emptyLabel, "EMPTY VIEW TEMPLATE");
					observations++;
					return (headerState.IsVisible && footerState.IsVisible && emptyState.IsVisible) ||
						observations >= 20;
				}, timeout: 5000, message: "The empty-state native template observation did not complete.");

				bool allTemplatesVisible =
					headerState.IsVisible &&
					footerState.IsVisible &&
					emptyState.IsVisible;

				Assert.True(allTemplatesVisible,
					$"CollectionView template visibility after empty transition: " +
					$"header(realized={headerState.Realized}, identity={headerState.Identity}, loaded={headerState.Loaded}, width={headerState.Width}, height={headerState.Height}); " +
					$"footer(realized={footerState.Realized}, identity={footerState.Identity}, loaded={footerState.Loaded}, width={footerState.Width}, height={footerState.Height}); " +
					$"empty(realized={emptyState.Realized}, identity={emptyState.Identity}, loaded={emptyState.Loaded}, width={emptyState.Width}, height={emptyState.Height}).");
			});
		}

		static TemplateState GetTemplateState(Label label, string expectedText)
		{
			if (label?.Handler?.PlatformView is not WFrameworkElement platformView)
				return default;

			return new TemplateState(
				true,
				label.Text == expectedText,
				platformView.IsLoaded,
				platformView.ActualWidth,
				platformView.ActualHeight);
		}

		readonly record struct TemplateState(bool Realized, bool Identity, bool Loaded, double Width, double Height)
		{
			public bool IsVisible => Realized && Identity && Loaded && Width > 0 && Height > 0;
		}
	}
}
#endif

