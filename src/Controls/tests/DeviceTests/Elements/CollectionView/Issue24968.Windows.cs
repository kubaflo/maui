#if WINDOWS
using System;
using System.Collections.ObjectModel;
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

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue24968")]
	public class Issue24968 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionViewRendersHeaderEmptyViewAndFooterTemplates()
		{
			const string headerText = "Cities";
			const string emptyText = "Empty";
			const string footerText = "Hello world !!!";
			const double largeFontSize = 32;
			Label header = null;
			Label emptyView = null;
			Label footer = null;
			bool headerLoaded = false;
			bool emptyViewLoaded = false;
			bool footerLoaded = false;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<HorizontalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			bool pageLoaded = false;
			var templatedCollectionView = CreateCollectionView();
			var templatedPage = CreatePage(templatedCollectionView);
			templatedPage.Loaded += (_, _) => pageLoaded = true;

			await CreateHandlerAndAddToWindow<LayoutHandler>(templatedPage, async _ =>
			{
				await AssertEventually(() => pageLoaded);
				Assert.True(pageLoaded, "The templated page did not complete its Loaded transition.");
				Assert.NotNull(templatedCollectionView.Handler);

				var handler = Assert.IsType<CollectionViewHandler>(templatedCollectionView.Handler);
				var listView = Assert.IsAssignableFrom<WListView>(handler.PlatformView);
				await AssertEventually(() => listView.ActualWidth > 0 && listView.ActualHeight > 0);
				await AssertEventually(() => headerLoaded && emptyViewLoaded && footerLoaded);

				Assert.NotNull(header);
				Assert.NotNull(emptyView);
				Assert.NotNull(footer);
				Assert.NotNull(templatedPage.Window);

				var visibleElements = templatedPage.Window.GetVisualTreeElements(
					0, 0, templatedPage.Window.Width, templatedPage.Window.Height);
				var missingTexts = new[]
				{
					(header, headerText),
					(emptyView, emptyText),
					(footer, footerText)
				}
				.Where(template => !visibleElements.Contains(template.Item1))
				.Select(template => template.Item2)
				.ToArray();
				Assert.True(
					!missingTexts.Any(),
					$"Issue24968: empty CollectionView templates are missing from the visible window. Missing=[{string.Join(", ", missingTexts)}]");
			});

			CollectionView CreateCollectionView()
			{
				var collectionView = new CollectionView
				{
					AutomationId = "TemplateCollectionView",
					ItemTemplate = new DataTemplate(() =>
					{
						var label = new Label { Padding = new Thickness(20, 5, 5, 5) };
						label.SetBinding(Label.TextProperty, ".");
						return label;
					})
				};

				collectionView.SetBinding(ItemsView.ItemsSourceProperty, nameof(IssueItemsSource.Items));

				collectionView.HeaderTemplate = new DataTemplate(() =>
				{
					header = new Label
					{
						Text = headerText,
						Padding = 10,
						FontAttributes = FontAttributes.Bold,
						FontSize = largeFontSize
					};
					header.Loaded += (_, _) => headerLoaded = true;
					return header;
				});
				collectionView.EmptyViewTemplate = new DataTemplate(() =>
				{
					emptyView = new Label
					{
						Text = emptyText,
						Padding = new Thickness(20, 5, 5, 5)
					};
					emptyView.Loaded += (_, _) => emptyViewLoaded = true;
					return emptyView;
				});
				collectionView.FooterTemplate = new DataTemplate(() =>
				{
					footer = new Label
					{
						Text = footerText,
						Padding = 10,
						FontAttributes = FontAttributes.Bold,
						FontSize = largeFontSize
					};
					footer.Loaded += (_, _) => footerLoaded = true;
					return footer;
				});

				return collectionView;
			}

			ContentPage CreatePage(CollectionView collectionView)
			{
				var grid = new Grid
				{
					Padding = 20,
					RowSpacing = 12,
					RowDefinitions =
					{
						new RowDefinition { Height = GridLength.Auto },
						new RowDefinition { Height = GridLength.Auto },
						new RowDefinition { Height = GridLength.Star }
					}
				};

				grid.Add(new Label
				{
					Text = "An empty CollectionView should show its templated header, empty view, and footer.",
					FontSize = 18
				});

				var probeRow = new HorizontalStackLayout
				{
					Spacing = 12
				};
				probeRow.Add(new Button
				{
					Text = "Check rendered templates",
					AutomationId = "CheckTemplatesButton"
				});
				probeRow.Add(new Label
				{
					Text = "CollectionView template check",
					AutomationId = "TemplateDescriptionLabel",
					VerticalOptions = LayoutOptions.Center
				});
				Grid.SetRow(probeRow, 1);
				grid.Add(probeRow);

				Grid.SetRow(collectionView, 2);
				grid.Add(collectionView);

				return new ContentPage
				{
					BindingContext = new IssueItemsSource(),
					Content = grid
				};
			}

		}

		sealed class IssueItemsSource
		{
			public ObservableCollection<string> Items { get; } = [];
		}
	}
}
#endif

