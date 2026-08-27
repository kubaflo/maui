#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29540")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue29540 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CustomTabbedViewHandlerInitializesDuringNavigation()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<NavigationPage, NavigationRenderer>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Toolbar, ToolbarHandler>();
					handlers.AddHandler<SwipeTabbedPage, SwipeTabbedPageHandler>();
				});
			});

			var previewGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Star }
				}
			};
			previewGrid.Add(new Label
			{
				HorizontalTextAlignment = TextAlignment.Center,
				Text = "First tab"
			}, 0, 0);
			previewGrid.Add(new Label
			{
				HorizontalTextAlignment = TextAlignment.Center,
				Text = "Second tab"
			}, 1, 0);

			var rootPage = new ContentPage
			{
				Title = "Custom TabbedPage handler",
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 18,
						Children =
						{
							new Label
							{
								FontAttributes = FontAttributes.Bold,
								FontSize = 24,
								Text = "SwipeTabbedPage with two child pages"
							},
							new Border
							{
								Padding = 16,
								Content = new VerticalStackLayout
								{
									Spacing = 10,
									Children =
									{
										new Label
										{
											FontAttributes = FontAttributes.Bold,
											Text = "Custom TabbedPage hierarchy"
										},
										previewGrid
									}
								}
							},
							new Label
							{
								Text = "The button navigates to a custom handler derived directly from TabbedViewHandler."
							},
							new Label
							{
								FontAttributes = FontAttributes.Bold,
								FontSize = 18,
								Text = "Ready"
							},
							new Button
							{
								Text = "Open custom TabbedPage"
							}
						}
					}
				}
			};
			var navigationPage = new NavigationPage(rootPage);

			var firstPage = new ContentPage
			{
				Title = "First tab",
				Content = new Label
				{
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					Text = "First tab content"
				}
			};
			var secondPage = new ContentPage
			{
				Title = "Second tab",
				Content = new Label
				{
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					Text = "Second tab content"
				}
			};
			var tabbedPage = new SwipeTabbedPage
			{
				Title = "SwipeTabbedPage",
				Children =
				{
					firstPage,
					secondPage
				}
			};

			Assert.Same(firstPage, tabbedPage.Children[0]);
			Assert.Same(secondPage, tabbedPage.Children[1]);
			Assert.Equal("First tab", tabbedPage.Children[0].Title);
			Assert.Equal("Second tab", tabbedPage.Children[1].Title);

			int handlerChangingState = -1;
			bool handlerChanged = false;
			tabbedPage.HandlerChanging += (_, args) =>
			{
				handlerChangingState =
					args.OldHandler is null && args.NewHandler is SwipeTabbedPageHandler ? 1 : 0;
			};
			tabbedPage.HandlerChanged += (_, _) => handlerChanged = true;

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(navigationPage), async _ =>
			{
				var navigationHandler = Assert.IsAssignableFrom<IPlatformViewHandler>(navigationPage.Handler);
				Assert.NotNull(navigationHandler.PlatformView);

				NotImplementedException observedException = null;
				try
				{
					await rootPage.Navigation.PushAsync(tabbedPage);
				}
				catch (NotImplementedException exception)
				{
					observedException = exception;
				}

				Assert.NotEqual(-1, handlerChangingState);
				Assert.Equal(1, handlerChangingState);
				Assert.True(
					observedException is null,
					$"Issue29540: custom TabbedViewHandler navigation threw {observedException?.GetType().FullName}: {observedException?.Message}");

				await AssertHelpers.AssertEventually(() => handlerChanged);
				Assert.Same(tabbedPage, navigationPage.CurrentPage);
				var tabbedHandler = Assert.IsType<SwipeTabbedPageHandler>(tabbedPage.Handler);
				Assert.NotNull(tabbedHandler.PlatformView);
				Assert.Same(firstPage, tabbedPage.Children[0]);
				Assert.Same(secondPage, tabbedPage.Children[1]);
			});
		}

		public sealed class SwipeTabbedPage : TabbedPage
		{
		}

		public sealed class SwipeTabbedPageHandler : Microsoft.Maui.Handlers.TabbedViewHandler
		{
		}
	}
}
#endif

