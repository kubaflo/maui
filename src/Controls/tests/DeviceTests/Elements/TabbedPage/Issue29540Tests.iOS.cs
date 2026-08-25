#if IOS && !MACCATALYST
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
	[Category(TestCategory.TabbedPage, "Issue29540")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue29540 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CustomTabbedViewHandlerCreatesAndAttachesPlatformView()
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
					handlers.AddHandler<SwipeTabbedPage, SwipeTabbedViewHandler>();
				});
			});

			var navigationPage = new NavigationPage(new ContentPage());
			var customPage = new SwipeTabbedPage
			{
				Title = "Swipe tabs",
				Children =
				{
					new ContentPage
					{
						Title = "First",
						Content = new Label
						{
							Text = "First tab",
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center
						}
					},
					new ContentPage
					{
						Title = "Second",
						Content = new Label
						{
							Text = "Second tab",
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center
						}
					}
				}
			};
			var initialStackCount = navigationPage.Navigation.NavigationStack.Count;
			var pushSettled = false;
			NotImplementedException pushException = null;

			await CreateHandlerAndAddToWindow(navigationPage, async () =>
			{
				try
				{
					await navigationPage.Navigation.PushAsync(customPage);
					pushSettled = true;
				}
				catch (NotImplementedException exception)
				{
					pushException = exception;
					pushSettled = true;
				}

				Assert.True(pushSettled, "The custom TabbedPage navigation attempt did not settle.");
				Assert.True(
					pushException is null,
					"Custom TabbedViewHandler navigation failed: observed System.NotImplementedException; expected successful navigation with an attached UIKit.UIView.");

				Assert.Equal(initialStackCount + 1, navigationPage.Navigation.NavigationStack.Count);
				Assert.Same(customPage, navigationPage.Navigation.NavigationStack[^1]);

				var customHandler = Assert.IsType<SwipeTabbedViewHandler>(customPage.Handler);
				var platformView = Assert.IsAssignableFrom<UIKit.UIView>(((IElementHandler)customHandler).PlatformView);
				Assert.NotNull(platformView.Window);
			});
		}

		public sealed class SwipeTabbedPage : TabbedPage
		{
		}

		public sealed class SwipeTabbedViewHandler : Microsoft.Maui.Handlers.TabbedViewHandler
		{
		}
	}
}
#endif

