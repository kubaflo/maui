#if IOS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29540")]
	public class Issue29540 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CustomTabbedViewHandlerInitializesAndAppears()
		{
			Assert.True(OperatingSystem.IsIOS());

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<NavigationPage, NavigationRenderer>();
					handlers.AddHandler<TabbedPage, TabbedRenderer>();
				});
			});

			var rootPage = new ContentPage();
			var navigationPage = new NavigationPage(rootPage);

			await CreateHandlerAndAddToWindow(new Window(navigationPage), async () =>
			{
				Assert.NotNull(rootPage.Handler);
				Assert.NotNull(((IPlatformViewHandler)rootPage.Handler).PlatformView);

				var tabbedPage = new SwipeTabbedPage
				{
					Title = "Custom tabs",
					Children =
					{
						new ContentPage
						{
							Title = "First",
							Content = new Label
							{
								Text = "First tab content",
								HorizontalOptions = LayoutOptions.Center,
								VerticalOptions = LayoutOptions.Center
							}
						},
						new ContentPage
						{
							Title = "Second",
							Content = new Label
							{
								Text = "Second tab content",
								HorizontalOptions = LayoutOptions.Center,
								VerticalOptions = LayoutOptions.Center
							}
						}
					}
				};

				var appearanceState = -1;
				var appeared = new TaskCompletionSource();
				tabbedPage.Appearing += OnAppearing;

				void OnAppearing(object sender, EventArgs args)
				{
					appearanceState = 1;
					appeared.TrySetResult();
					tabbedPage.Appearing -= OnAppearing;
				}

				var handler = new SwipeTabbedViewHandler();

				try
				{
					handler.SetMauiContext(rootPage.Handler.MauiContext);
					tabbedPage.Handler = handler;
					handler.SetVirtualView(tabbedPage);

					Assert.NotNull(handler.PlatformView);

					await navigationPage.PushAsync(tabbedPage);
					await appeared.Task.WaitAsync(TimeSpan.FromSeconds(2));
				}
				catch (NotImplementedException)
				{
					Assert.Fail("Issue 29540: custom TabbedViewHandler initialization threw System.NotImplementedException");
				}

				Assert.Equal(1, appearanceState);
				Assert.Same(tabbedPage, navigationPage.Navigation.NavigationStack[1]);
			});
		}

		sealed class SwipeTabbedPage : TabbedPage
		{
		}

		sealed class SwipeTabbedViewHandler : TabbedViewHandler
		{
		}
	}
}
#endif

