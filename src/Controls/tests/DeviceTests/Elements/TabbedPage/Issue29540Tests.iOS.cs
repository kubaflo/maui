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
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29540")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue29540 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DerivedTabbedViewHandlerInitializesAndNavigates()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<NavigationPage, NavigationRenderer>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Toolbar, ToolbarHandler>();
					handlers.AddHandler<TabbedPage, TabbedViewHandler>();
				});
			});

			var rootPage = new ContentPage();
			var navigationPage = new NavigationPage(rootPage);
			var tabbedPage = new SwipeTabbedPage
			{
				Children =
				{
					new ContentPage
					{
						Title = "First tab",
						Content = new Label
						{
							Text = "First tab content",
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center
						}
					},
					new ContentPage
					{
						Title = "Second tab",
						Content = new Label
						{
							Text = "Second tab content",
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center
						}
					}
				}
			};

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(navigationPage), async _ =>
			{
				Assert.NotNull(rootPage.Handler);
				Assert.NotNull(rootPage.Handler.MauiContext);

				var customHandler = new CustomTabbedViewHandler();
				NotImplementedException initializationException = null;

				try
				{
					customHandler.SetMauiContext(rootPage.Handler.MauiContext);
					customHandler.SetVirtualView(tabbedPage);
				}
				catch (NotImplementedException exception)
				{
					initializationException = exception;
				}

				Assert.True(
					initializationException is null,
					$"Custom TabbedViewHandler initialization should complete without exception; observed {initializationException?.GetType().FullName}");
				Assert.NotNull(((IElementHandler)customHandler).PlatformView);

				bool navigatedTo = false;
				tabbedPage.NavigatedTo += OnNavigatedTo;

				await navigationPage.PushAsync(tabbedPage);
				await AssertEventually(() => navigatedTo);

				var navigationStack = navigationPage.Navigation.NavigationStack;
				Assert.Same(tabbedPage, navigationStack[navigationStack.Count - 1]);
				Assert.Same(customHandler, tabbedPage.Handler);

				void OnNavigatedTo(object sender, NavigatedToEventArgs args)
				{
					navigatedTo = true;
					tabbedPage.NavigatedTo -= OnNavigatedTo;
				}
			});
		}

		sealed class SwipeTabbedPage : TabbedPage
		{
		}

		sealed class CustomTabbedViewHandler : Microsoft.Maui.Handlers.TabbedViewHandler
		{
		}
	}
}
#endif

