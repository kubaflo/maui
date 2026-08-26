using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue27332")]
	public class Issue27332 : ControlsHandlerTestBase
	{
#if WINDOWS
		[Fact]
		public async Task EmptyCollectionFooterImmediatelyFollowsHeader()
		{
			const double gapTolerance = 4;
			var items = new ObservableCollection<string>();
			var resultStatus = new Label
			{
				Text = "Collection status",
				FontAttributes = FontAttributes.Bold
			};
			var interactionStatus = new Label
			{
				Text = "Interaction status"
			};
			var headerLabel = new Label
			{
				Margin = new Thickness(10, 0, 0, 0),
				Text = "Header",
				FontSize = 12,
				FontAttributes = FontAttributes.Bold
			};
			var footerLabel = new Label
			{
				Margin = new Thickness(10, 0, 0, 0),
				Text = "Footer",
				FontSize = 12,
				FontAttributes = FontAttributes.Bold
			};

#pragma warning disable CS0618
			var collectionView = new CollectionView
			{
				VerticalOptions = LayoutOptions.FillAndExpand,
				ItemsSource = items,
				Header = new StackLayout
				{
					BackgroundColor = Colors.LightGray,
					Children = { headerLabel }
				},
				Footer = new StackLayout
				{
					BackgroundColor = Colors.LightGray,
					Children = { footerLabel }
				},
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label
					{
						FontSize = Device.GetNamedSize(NamedSize.Title, typeof(Label))
					};
					itemLabel.SetBinding(Label.TextProperty, ".");

					return new Grid
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
						},
						Children =
						{
							new ContentView
							{
								Content = new Frame
								{
									Margin = new Thickness(2, 6),
									CornerRadius = 20,
									Content = itemLabel
								}
							}
						}
					};
				})
			};
#pragma warning restore CS0618

			var statusLayout = new StackLayout
			{
				Children =
				{
					new Label { Text = "The header and footer should be adjacent when the collection is empty." },
					resultStatus,
					interactionStatus
				}
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
			pageGrid.Add(statusLayout, 0, 0);
			pageGrid.Add(addButton, 0, 1);
			pageGrid.Add(clearButton, 0, 1);
			pageGrid.Add(collectionView, 0, 2);
			var testPage = new ContentPage { Content = pageGrid };

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<StackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
					handlers.AddHandler<ContentView, ContentViewHandler>();
#pragma warning disable CS0618
					handlers.AddHandler<Frame, FrameRenderer>();
#pragma warning restore CS0618
				});
			});

			bool nativeLoaded = false;
			bool layoutUpdatedAfterLoad = false;
			collectionView.HandlerChanged += (_, _) =>
			{
				Assert.NotNull(collectionView.Handler);
				var platformCollection = Assert.IsAssignableFrom<WFrameworkElement>(collectionView.Handler.PlatformView);
				platformCollection.Loaded += (_, _) =>
				{
					nativeLoaded = true;
					platformCollection.LayoutUpdated += (_, _) => layoutUpdatedAfterLoad = true;
				};
			};

			await CreateHandlerAndAddToWindow(testPage, async () =>
			{
				await AssertEventually(
					() => nativeLoaded && layoutUpdatedAfterLoad,
					timeout: 5000,
					message: "The native CollectionView did not complete a post-attachment layout.");

				Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				Assert.Equal("Header", headerLabel.Text);
				Assert.Equal("Footer", footerLabel.Text);

				Assert.NotNull(testPage.Handler);
				var nativeRoot = Assert.IsAssignableFrom<WFrameworkElement>(testPage.Handler.PlatformView);
				Assert.True(nativeRoot.ActualWidth > 0);
				Assert.True(nativeRoot.ActualHeight > 0);

				(double X, double Y, double Width, double Height) GetNativeFrame(Label label)
				{
					Assert.NotNull(label.Handler);
					var nativeLabel = Assert.IsAssignableFrom<WFrameworkElement>(label.Handler.PlatformView);
					Assert.True(nativeLabel.ActualWidth > 0);
					Assert.True(nativeLabel.ActualHeight > 0);

					var origin = nativeLabel.TransformToVisual(nativeRoot).TransformPoint(default);
					Assert.True(origin.X >= 0 && origin.Y >= 0);
					Assert.True(origin.X + nativeLabel.ActualWidth <= nativeRoot.ActualWidth + gapTolerance);
					Assert.True(origin.Y + nativeLabel.ActualHeight <= nativeRoot.ActualHeight + gapTolerance);
					return (origin.X, origin.Y, nativeLabel.ActualWidth, nativeLabel.ActualHeight);
				}

				var resultFrame = GetNativeFrame(resultStatus);
				var interactionFrame = GetNativeFrame(interactionStatus);
				double statusGap = interactionFrame.Y - (resultFrame.Y + resultFrame.Height);
				Assert.InRange(statusGap, -gapTolerance, gapTolerance);

				var headerFrame = GetNativeFrame(headerLabel);
				var footerFrame = GetNativeFrame(footerLabel);
				double headerBottom = headerFrame.Y + headerFrame.Height;
				double footerTop = footerFrame.Y;
				double gap = footerTop - headerBottom;

				Assert.True(
					gap <= gapTolerance,
					$"Issue27332 footer must immediately follow header: native gap was {gap:F2}px, expected 0 +/- {gapTolerance:F2}px; header bottom was {headerBottom:F2}px and footer top was {footerTop:F2}px.");
			});
		}
#endif
	}
}

