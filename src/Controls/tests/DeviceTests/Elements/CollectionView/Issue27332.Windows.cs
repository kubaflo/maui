#pragma warning disable CS0618 // Frame is required to reproduce the reported hierarchy.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
#if WINDOWS
	[Category("Issue27332")]
	public class Issue27332 : ControlsHandlerTestBase
	{
		const double AdjacencyLimit = 4;
		const double MeasurementTolerance = 1;

		[Fact]
		public async Task EmptyCollectionViewFooterIsAdjacentToHeader()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<StackLayout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<ContentView, ContentViewHandler>();
				});
			});

			var calibrationHeader = CreateHeaderOrFooter("Header");
			var calibrationFooter = CreateHeaderOrFooter("Footer");
			var calibrationPage = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Spacing = 0,
					Children =
					{
						calibrationHeader.Container,
						calibrationFooter.Container
					}
				}
			};

			var calibration = await MeasureAttachedAsync(
				calibrationPage,
				calibrationHeader.Container,
				calibrationFooter.Container);
			var calibrationGap = calibration.FooterTop - calibration.HeaderBottom;

			Assert.True(
				Math.Abs(calibrationGap) <= MeasurementTolerance,
				$"Native frame calibration expected a zero gap, but measured header={calibration.HeaderTop:F2}-{calibration.HeaderBottom:F2}, footer={calibration.FooterTop:F2}-{calibration.FooterBottom:F2}, gap={calibrationGap:F2}.");

			var items = new ObservableCollection<string>();
			var header = CreateHeaderOrFooter("Header");
			var footer = CreateHeaderOrFooter("Footer");
			var collectionView = new CollectionView
			{
				ItemsSource = items,
				Header = header.Container,
				Footer = footer.Container,
				VerticalOptions = LayoutOptions.FillAndExpand,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label
					{
						FontSize = (double)new FontSizeConverter().ConvertFromInvariantString("Title")
					};
					itemLabel.SetBinding(Label.TextProperty, ".");

					var frame = new Frame
					{
						Margin = new Thickness(2, 6),
						CornerRadius = 20,
						Content = itemLabel
					};

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
					itemGrid.Add(new ContentView { Content = frame });
					return itemGrid;
				})
			};

			var instructions = new StackLayout
			{
				Children =
				{
					new Label
					{
						Text = "CollectionView header and footer layout."
					}
				}
			};

			var addButton = new Button
			{
				Text = "Add 2 Items",
				FontAttributes = FontAttributes.Bold,
				Margin = 20,
				HorizontalOptions = LayoutOptions.Start
			};
			SemanticProperties.SetHint(addButton, "Counts the number of times you click");

			var clearButton = new Button
			{
				Text = "Clear All Items",
				FontAttributes = FontAttributes.Bold,
				Margin = 20,
				HorizontalOptions = LayoutOptions.End
			};
			SemanticProperties.SetHint(clearButton, "Counts the number of times you click");

			var checkLayout = new VerticalStackLayout
			{
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Button { Text = "Check layout" },
					new Label
					{
						Text = "Header / Footer",
						FontAttributes = FontAttributes.Bold,
						HorizontalTextAlignment = TextAlignment.Center
					}
				}
			};

			var rootGrid = new Grid
			{
				Margin = 20,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			rootGrid.Add(instructions);
			rootGrid.Add(addButton, 0, 1);
			rootGrid.Add(clearButton, 0, 1);
			rootGrid.Add(checkLayout, 0, 1);
			rootGrid.Add(collectionView, 0, 2);

			var page = new ContentPage
			{
				Title = "Header and Footer (Add Clear)",
				Content = rootGrid
			};

			Assert.Empty(items);
			Assert.Same(header.Container, collectionView.Header);
			Assert.Same(footer.Container, collectionView.Footer);
			Assert.Equal("Header", header.Text.Text);
			Assert.Equal("Footer", footer.Text.Text);

			var measured = await MeasureAttachedAsync(page, header.Container, footer.Container);
			var gap = measured.FooterTop - measured.HeaderBottom;

			Assert.True(
				gap <= AdjacencyLimit + MeasurementTolerance,
				$"Windows CollectionView footer should be adjacent to its header when the ItemsSource is empty; header={measured.HeaderTop:F2}-{measured.HeaderBottom:F2}, footer={measured.FooterTop:F2}-{measured.FooterBottom:F2}, gap={gap:F2}, limit={AdjacencyLimit:F2}.");
		}

		async Task<NativeFrames> MeasureAttachedAsync(
			ContentPage page,
			StackLayout header,
			StackLayout footer)
		{
			var pageLoaded = false;
			var headerLoaded = false;
			var footerLoaded = false;
			var layoutCompleted = false;
			var pageLoadedSource = new TaskCompletionSource();
			var headerLoadedSource = new TaskCompletionSource();
			var footerLoadedSource = new TaskCompletionSource();
			var layoutSource = new TaskCompletionSource();
			var frames = new NativeFrames(-1, -1, -1, -1);

			page.Loaded += (_, _) =>
			{
				pageLoaded = true;
				pageLoadedSource.TrySetResult();
			};
			header.Loaded += (_, _) =>
			{
				headerLoaded = true;
				headerLoadedSource.TrySetResult();
			};
			footer.Loaded += (_, _) =>
			{
				footerLoaded = true;
				footerLoadedSource.TrySetResult();
			};
			page.SizeChanged += (_, _) =>
			{
				if (page.Width > 0 && page.Height > 0)
				{
					layoutCompleted = true;
					layoutSource.TrySetResult();
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var timeout = TimeSpan.FromSeconds(5);
				await pageLoadedSource.Task.WaitAsync(timeout);
				await headerLoadedSource.Task.WaitAsync(timeout);
				await footerLoadedSource.Task.WaitAsync(timeout);
				await layoutSource.Task.WaitAsync(timeout);

				Assert.True(pageLoaded, "The test page did not report that it was loaded.");
				Assert.True(headerLoaded, "The header did not report that it was loaded.");
				Assert.True(footerLoaded, "The footer did not report that it was loaded.");
				Assert.True(layoutCompleted, "The attached page did not complete a positive-size layout.");

				Assert.NotNull(page.Handler);
				Assert.NotNull(header.Handler);
				Assert.NotNull(footer.Handler);
				Assert.NotNull(page.Handler.PlatformView);
				Assert.NotNull(header.Handler.PlatformView);
				Assert.NotNull(footer.Handler.PlatformView);

				var rootFrame = ((IView)page).GetBoundingBox();
				var headerFrame = ((IView)header).GetBoundingBox();
				var footerFrame = ((IView)footer).GetBoundingBox();

				Assert.True(page.IsLoaded && rootFrame.Width > 0 && rootFrame.Height > 0);
				Assert.True(header.IsLoaded && headerFrame.Width > 0 && headerFrame.Height > 0);
				Assert.True(footer.IsLoaded && footerFrame.Width > 0 && footerFrame.Height > 0);

				frames = new NativeFrames(
					headerFrame.Top,
					headerFrame.Bottom,
					footerFrame.Top,
					footerFrame.Bottom);

				Assert.True(frames.HeaderTop >= rootFrame.Top - MeasurementTolerance);
				Assert.True(frames.HeaderBottom <= rootFrame.Bottom + MeasurementTolerance);
				Assert.True(frames.FooterTop >= rootFrame.Top - MeasurementTolerance);
				Assert.True(frames.FooterBottom <= rootFrame.Bottom + MeasurementTolerance);
				Assert.True(frames.HeaderTop < frames.HeaderBottom);
				Assert.True(frames.FooterTop < frames.FooterBottom);
				Assert.True(frames.HeaderTop <= frames.FooterTop);
			});

			return frames;
		}

		static HeaderOrFooter CreateHeaderOrFooter(string text)
		{
			var label = new Label
			{
				Margin = new Thickness(10, 0, 0, 0),
				Text = text,
				FontSize = 12,
				FontAttributes = FontAttributes.Bold
			};

			return new HeaderOrFooter(
				new StackLayout
				{
					BackgroundColor = Colors.LightGray,
					Children = { label }
				},
				label);
		}

		readonly record struct HeaderOrFooter(StackLayout Container, Label Text);

		readonly record struct NativeFrames(
			double HeaderTop,
			double HeaderBottom,
			double FooterTop,
			double FooterBottom);
	}
#endif
}

