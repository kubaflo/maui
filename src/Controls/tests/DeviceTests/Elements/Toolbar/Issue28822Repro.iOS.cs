using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if IOS && !MACCATALYST
	[Category("Issue28822")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue28822 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task FilenameBackedToolbarItemPreservesSourceColors()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler(typeof(Toolbar), typeof(ToolbarHandler));
					handlers.AddHandler(typeof(NavigationPage), typeof(NavigationRenderer));
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
				});
			});

			var catalogToolbarItem = new ToolbarItem
			{
				Text = "Catalog",
				IconImageSource = "appicon",
				Order = ToolbarItemOrder.Primary,
				Priority = 0,
				AutomationId = "Issue28822Catalog"
			};
			var filenameToolbarItem = new ToolbarItem
			{
				Text = "MauiImage",
				IconImageSource = "red.png",
				Order = ToolbarItemOrder.Primary,
				Priority = 1,
				AutomationId = "Issue28822Filename"
			};
			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					new Image
					{
						Source = "red.png",
						HeightRequest = 180,
						Aspect = Aspect.AspectFit
					}
				},
				ToolbarItems =
				{
					catalogToolbarItem,
					filenameToolbarItem
				}
			};
			var navigationPage = new NavigationPage(page);
			var filenameMode = (UIImageRenderingMode)(-1);
			var filenameImageObserved = false;
			UIBarButtonItem catalogNativeItem = null;
			UIBarButtonItem filenameNativeItem = null;

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(navigationPage), async _ =>
			{
				var navigationController = (UINavigationController)navigationPage.Handler;

				await AssertEventually(() =>
				{
					var nativeItems = navigationController.TopViewController?.NavigationItem.RightBarButtonItems;
					if (nativeItems is not { Length: 2 })
						return false;

					foreach (var nativeItem in nativeItems)
					{
						if (nativeItem.AccessibilityIdentifier == filenameToolbarItem.AutomationId)
							filenameNativeItem = nativeItem;
						else if (nativeItem.AccessibilityIdentifier == catalogToolbarItem.AutomationId)
							catalogNativeItem = nativeItem;
					}

					if (filenameNativeItem?.Image is null || catalogNativeItem is null)
						return false;

					filenameMode = filenameNativeItem.Image.RenderingMode;
					filenameImageObserved = true;
					return true;
				}, timeout: 5000, message: "filename-backed ToolbarItem image did not load");

				Assert.True(filenameImageObserved);
				Assert.NotNull(filenameNativeItem.Image);
				Assert.Equal("Issue28822Filename", filenameNativeItem.AccessibilityIdentifier);
				Assert.Equal("Issue28822Catalog", catalogNativeItem.AccessibilityIdentifier);
				var orderedNativeItems = navigationController.TopViewController.NavigationItem.RightBarButtonItems;
				Assert.NotNull(orderedNativeItems);
				Assert.Equal("Issue28822Filename", orderedNativeItems[0].AccessibilityIdentifier);
				Assert.Equal("Issue28822Catalog", orderedNativeItems[1].AccessibilityIdentifier);
				Assert.True(
					filenameMode == UIImageRenderingMode.AlwaysOriginal,
					$"filename-backed ToolbarItem image should preserve source colors; actual rendering mode was {filenameMode}.");
			});
		}
	}
#endif
}

