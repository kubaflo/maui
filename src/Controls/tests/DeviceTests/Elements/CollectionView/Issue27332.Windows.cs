#if WINDOWS
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WListView = Microsoft.UI.Xaml.Controls.ListView;
using WPanel = Microsoft.UI.Xaml.Controls.Panel;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WAutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(RunInNewWindowCollection)]
	[Category("Issue27332")]
	public class Issue27332 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionFooterImmediatelyFollowsHeader()
		{
			const double tolerance = 1;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<StackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<ContentView, ContentViewHandler>();
#pragma warning disable CS0618 // The reported item template uses Frame.
					handlers.AddHandler<Frame, FrameRenderer>();
#pragma warning restore CS0618
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			(StackLayout Container, Label Text) CreateStructuralItem(string text, string automationId)
			{
				var label = new Label
				{
					AutomationId = automationId,
					Margin = new Thickness(10, 0, 0, 0),
					Text = text,
					FontSize = 12,
					FontAttributes = FontAttributes.Bold
				};

				return (new StackLayout
				{
					BackgroundColor = Colors.LightGray,
					Children = { label }
				}, label);
			}

			Rect GetNativeFrame(Label label, WFrameworkElement relativeTo)
			{
				var nativeLabel = label.ToPlatform() as WFrameworkElement;
				Assert.NotNull(nativeLabel);
				var location = nativeLabel.GetLocationRelativeTo(relativeTo);
				Assert.True(location.HasValue, $"A native position was not available for {label.Text}.");
				return new Rect(location.Value.X, location.Value.Y, nativeLabel.ActualWidth, nativeLabel.ActualHeight);
			}

			void AssertNativeStructuralItem(
				(StackLayout Container, Label Text) item,
				string expectedText,
				string expectedAutomationId)
			{
				var nativeLabel = item.Text.ToPlatform() as WTextBlock;
				var nativeContainer = item.Container.ToPlatform() as WPanel;
				Assert.NotNull(nativeLabel);
				Assert.NotNull(nativeContainer);
				Assert.True(nativeLabel.IsLoaded());
				Assert.True(nativeContainer.IsLoaded());
				Assert.Equal(Microsoft.UI.Xaml.Visibility.Visible, nativeLabel.Visibility);
				Assert.Equal(Microsoft.UI.Xaml.Visibility.Visible, nativeContainer.Visibility);
				Assert.Equal(expectedText, nativeLabel.Text);
				Assert.Equal(expectedAutomationId, item.Text.AutomationId);
				Assert.Equal(expectedAutomationId, WAutomationProperties.GetAutomationId(nativeLabel));
				Assert.Equal(12d, nativeLabel.FontSize);
				Assert.Equal(Microsoft.UI.Text.FontWeights.Bold.Weight, nativeLabel.FontWeight.Weight);
				Assert.Equal(10d, item.Text.Margin.Left);

				var background = nativeContainer.Background as WSolidColorBrush;
				Assert.NotNull(background);
				Assert.Equal(211, background.Color.R);
				Assert.Equal(211, background.Color.G);
				Assert.Equal(211, background.Color.B);
				Assert.Equal(255, background.Color.A);
			}

			var items = new ObservableCollection<string>();
			var header = CreateStructuralItem("Header", "HeaderTarget");
			var footer = CreateStructuralItem("Footer", "FooterTarget");

#pragma warning disable CS0618 // The reported layout uses FillAndExpand and Frame.
			var collectionView = new CollectionView
			{
				AutomationId = "CV",
				VerticalOptions = LayoutOptions.FillAndExpand,
				ItemsSource = items,
				Header = header.Container,
				Footer = footer.Container,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label
					{
						FontSize = Device.GetNamedSize(NamedSize.Title, typeof(Label))
					};
					itemLabel.SetBinding(Label.TextProperty, ".");

					var frame = new Frame
					{
						Margin = new Thickness(2, 6),
						CornerRadius = 20,
						Content = itemLabel
					};
					var contentView = new ContentView { Content = frame };
					var itemGrid = new Grid
					{
						Padding = 10,
						RowDefinitions =
						{
							new RowDefinition(GridLength.Auto),
							new RowDefinition(GridLength.Auto)
						},
						ColumnDefinitions =
						{
							new ColumnDefinition(GridLength.Auto),
							new ColumnDefinition(GridLength.Auto)
						}
					};
					itemGrid.Add(contentView);
					return itemGrid;
				})
			};
#pragma warning restore CS0618

			var instructionStack = new StackLayout
			{
				new Label { Text = "CollectionView header and footer layout" },
				new Label { Text = "Collection is empty", FontAttributes = FontAttributes.Bold }
			};
			var addButton = new Button
			{
				Text = "Add 2 Items",
				FontAttributes = FontAttributes.Bold,
				Margin = 20,
				HorizontalOptions = LayoutOptions.Start
			};
			var clearButton = new Button
			{
				Text = "Clear All Items",
				FontAttributes = FontAttributes.Bold,
				Margin = 20,
				HorizontalOptions = LayoutOptions.End
			};
			var pageGrid = new Grid
			{
				Margin = 20,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			pageGrid.Add(instructionStack);
			pageGrid.Add(addButton);
			pageGrid.Add(clearButton);
			pageGrid.Add(collectionView);
			Grid.SetRow(instructionStack, 0);
			Grid.SetRow(addButton, 1);
			Grid.SetRow(clearButton, 1);
			Grid.SetRow(collectionView, 2);

			var page = new ContentPage { Content = pageGrid };
			var collectionLoadedState = -1;
			collectionView.Loaded += (_, _) => collectionLoadedState = 1;

			await CreateHandlerAndAddToWindow<PageHandler>(page, async _ =>
			{
				await AssertEventually(() =>
				{
					var platformCollection = collectionView.Handler?.PlatformView as WListView;
					var platformHeader = header.Text.Handler?.PlatformView as WFrameworkElement;
					var platformFooter = footer.Text.Handler?.PlatformView as WFrameworkElement;
					return collectionLoadedState == 1 &&
						platformCollection is not null &&
						platformCollection.IsLoaded() &&
						platformCollection.ActualWidth > 0 &&
						platformCollection.ActualHeight > 0 &&
						platformHeader is not null &&
						platformHeader.IsLoaded() &&
						platformHeader.ActualHeight > 0 &&
						platformFooter is not null &&
						platformFooter.IsLoaded() &&
						platformFooter.ActualHeight > 0;
				}, timeout: 10000);

				Assert.Equal(1, collectionLoadedState);
				Assert.Empty(items);

				var nativeCollection = collectionView.Handler.PlatformView as WListView;
				Assert.NotNull(nativeCollection);
				AssertNativeStructuralItem(header, "Header", "HeaderTarget");
				AssertNativeStructuralItem(footer, "Footer", "FooterTarget");

				var headerFrame = GetNativeFrame(header.Text, nativeCollection);
				var footerFrame = GetNativeFrame(footer.Text, nativeCollection);
				Assert.True(headerFrame.Width > 0 && headerFrame.Height > 0);
				Assert.True(footerFrame.Width > 0 && footerFrame.Height > 0);
				Assert.True(headerFrame.Left >= -tolerance && headerFrame.Right <= nativeCollection.ActualWidth + tolerance);
				Assert.True(footerFrame.Left >= -tolerance && footerFrame.Right <= nativeCollection.ActualWidth + tolerance);
				Assert.True(headerFrame.Top >= -tolerance && headerFrame.Bottom <= nativeCollection.ActualHeight + tolerance);
				Assert.True(footerFrame.Top >= -tolerance && footerFrame.Bottom <= nativeCollection.ActualHeight + tolerance);

				var gap = footerFrame.Top - headerFrame.Bottom;
				Assert.True(
					Math.Abs(gap) <= tolerance,
					$"Issue27332 footer should immediately follow header; measured gap={gap:F2}, header={headerFrame}, footer={footerFrame}, expected gap=0.00, tolerance={tolerance:F2}, CollectionView height={nativeCollection.ActualHeight:F2}.");
			});
		}
	}
}
#endif

