using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WAppBarButton = Microsoft.UI.Xaml.Controls.AppBarButton;
using WColor = global::Windows.UI.Color;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue34071")]
public class Issue34071 : ControlsHandlerTestBase
{
#if WINDOWS
	[Fact]
	public async Task ShellForegroundColorAppliesToPrimaryToolbarItem()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.SetupShellHandlers();
				handlers.AddHandler<Controls.Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Image, ImageHandler>();
			});
		});

		var loaded = false;
		WAppBarButton toolbarButton = null;
		var expectedColor = Colors.Purple;
		var shell = new Shell();
		Shell.SetForegroundColor(shell, expectedColor);

		shell.Items.Add(new ShellContent
		{
			Title = "Home",
			Route = "MainPage",
			ContentTemplate = new DataTemplate(() =>
			{
				var page = new ContentPage
				{
					Title = "Home"
				};
				page.ToolbarItems.Add(new ToolbarItem
				{
					AutomationId = "AffectedToolbarIcon",
					IconImageSource = "red.png",
					Order = ToolbarItemOrder.Primary
				});
				page.Loaded += (_, _) => loaded = true;
				return page;
			})
		});

		await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Controls.Window(shell), async _ =>
		{
			await AssertEventually(() => loaded);
			Assert.True(loaded);

			await AssertEventually(() =>
			{
				toolbarButton = FindToolbarButton((WDependencyObject)shell.Handler.PlatformView);
				return toolbarButton is not null;
			});

			Assert.NotNull(toolbarButton);
			await AssertEventually(() =>
				toolbarButton.Content is WImage image &&
				image.Source is not null &&
				toolbarButton.ActualWidth > 0 &&
				toolbarButton.ActualHeight > 0);

			var nativeIcon = Assert.IsType<WImage>(toolbarButton.Content);
			Assert.NotNull(nativeIcon.Source);
			Assert.True(toolbarButton.ActualWidth > 0);
			Assert.True(toolbarButton.ActualHeight > 0);

			var expected = expectedColor.ToWindowsColor();
			var actualBrush = Assert.IsType<WSolidColorBrush>(toolbarButton.Foreground);
			var actual = actualBrush.Color;
			Assert.True(
				ColorsMatch(actual, expected),
				$"Shell ToolbarItem foreground mismatch: observed A={actual.A}, R={actual.R}, G={actual.G}, B={actual.B}; expected A={expected.A}, R={expected.R}, G={expected.G}, B={expected.B}.");
		});
	}
#endif

	static WAppBarButton FindToolbarButton(WDependencyObject element)
	{
		if (element is WAppBarButton button &&
			Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(button) == "AffectedToolbarIcon")
		{
			return button;
		}

		for (var i = 0; i < WVisualTreeHelper.GetChildrenCount(element); i++)
		{
			var match = FindToolbarButton(WVisualTreeHelper.GetChild(element, i));
			if (match is not null)
				return match;
		}

		return null;
	}

	static bool ColorsMatch(WColor actual, WColor expected)
	{
		const int tolerance = 1;
		return Math.Abs(actual.A - expected.A) <= tolerance &&
			Math.Abs(actual.R - expected.R) <= tolerance &&
			Math.Abs(actual.G - expected.G) <= tolerance &&
			Math.Abs(actual.B - expected.B) <= tolerance;
	}
}

