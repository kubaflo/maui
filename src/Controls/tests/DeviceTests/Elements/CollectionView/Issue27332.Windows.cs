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
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue27332")]
	public class Issue27332 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionFooterRendersDirectlyAfterHeader()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<StackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var header = CreateHeaderOrFooter("Header");
			var footer = CreateHeaderOrFooter("Footer");
			var headerLabel = (Label)header.Children[0];
			var footerLabel = (Label)footer.Children[0];
			var items = new ObservableCollection<string>();

#pragma warning disable CS0618
			var collectionView = new CollectionView
			{
				Header = header,
				Footer = footer,
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label();
					itemLabel.SetBinding(Label.TextProperty, new Binding("."));
					return new Frame
					{
						Margin = 5,
						Padding = 10,
						Content = itemLabel
					};
				})
			};
#pragma warning restore CS0618

			var instructionRow = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Auto)
				}
			};
			instructionRow.Add(new Label
			{
				Text = "Header and Footer",
				VerticalOptions = LayoutOptions.Center
			});
			instructionRow.Add(new Label
			{
				Text = "Add and Clear",
				FontAttributes = FontAttributes.Bold,
				VerticalOptions = LayoutOptions.Center
			}, 1);

			var buttonRow = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				}
			};
			buttonRow.Add(new Button
			{
				Text = "Add 2 Items",
				HorizontalOptions = LayoutOptions.Start
			});
			buttonRow.Add(new Button
			{
				Text = "Clear All Items",
				HorizontalOptions = LayoutOptions.End
			}, 1);

			var root = new Grid
			{
				Padding = 20,
				RowSpacing = 20,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			root.Add(instructionRow);
			root.Add(buttonRow);
			Grid.SetRow(buttonRow, 1);
			root.Add(collectionView);
			Grid.SetRow(collectionView, 2);

			var page = new ContentPage
			{
				Title = "Header and Footer (Add Clear)",
				Content = root
			};

			var loaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			int loadedCallbackCount = -1;
			collectionView.Loaded += (_, _) =>
			{
				loadedCallbackCount++;
				loaded.TrySetResult(true);
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await loaded.Task.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.True(loadedCallbackCount > -1, "CollectionView did not raise its loaded callback.");
				Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				Assert.Same(collectionView, collectionView.Handler.VirtualView);
				Assert.Empty(items);
				Assert.Equal(Colors.LightGray, header.BackgroundColor);
				Assert.Equal(Colors.LightGray, footer.BackgroundColor);
				Assert.Equal("Header", headerLabel.Text);
				Assert.Equal("Footer", footerLabel.Text);
				Assert.Equal(FontAttributes.Bold, headerLabel.FontAttributes);
				Assert.Equal(FontAttributes.Bold, footerLabel.FontAttributes);
				Assert.NotSame(header.ToPlatform(), footer.ToPlatform());

				var collectionBounds = ((IView)collectionView).GetBoundingBox();
				var headerBounds = ((IView)header).GetBoundingBox();
				var footerBounds = ((IView)footer).GetBoundingBox();

				Assert.True(collectionBounds.Width > 0 && collectionBounds.Height > 0,
					"CollectionView should have a positive rendered size.");
				Assert.True(headerBounds.Width > 0 && headerBounds.Height > 0,
					"Header should have a positive rendered size.");
				Assert.True(footerBounds.Width > 0 && footerBounds.Height > 0,
					"Footer should have a positive rendered size.");
				Assert.True(headerBounds.Top >= collectionBounds.Top - 1 &&
					headerBounds.Bottom <= collectionBounds.Bottom + 1,
					"Header should be rendered within the CollectionView surface.");
				Assert.True(footerBounds.Top >= collectionBounds.Top - 1 &&
					footerBounds.Bottom <= collectionBounds.Bottom + 1,
					"Footer should be rendered within the CollectionView surface.");

				double renderedGap = footerBounds.Top - headerBounds.Bottom;
				Assert.True(Math.Abs(renderedGap) <= 1,
					"CollectionView footer should render directly after its header when ItemsSource is empty");
			});
		}

		static StackLayout CreateHeaderOrFooter(string text) =>
			new()
			{
				BackgroundColor = Colors.LightGray,
				Children =
				{
					new Label
					{
						Margin = new Thickness(10, 0, 0, 0),
						Text = text,
						FontAttributes = FontAttributes.Bold
					}
				}
			};
	}
}

