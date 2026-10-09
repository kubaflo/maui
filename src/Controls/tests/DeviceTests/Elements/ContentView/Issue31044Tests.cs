#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WButtonAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer;
using WIInvokeProvider = Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider;
using WPatternInterface = Microsoft.UI.Xaml.Automation.Peers.PatternInterface;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue31044")]
	public class Issue31044 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ReplacedControlTemplateDisconnectsHandlers()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var templateOneLoaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var templateOneUnloaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var templateTwoLoaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var toggleClicked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			ContentView templateOneRoot = null;
			Label templateOneChild = null;
			ContentView templateTwoRoot = null;

			var templateOne = new ControlTemplate(() =>
			{
				templateOneChild = new Label
				{
					AutomationId = "TemplateOneContent",
					Text = "Template 1 content"
				};
				templateOneRoot = new ContentView
				{
					AutomationId = "TemplateOneRoot",
					Content = templateOneChild
				};
				templateOneRoot.Loaded += (_, _) => templateOneLoaded.TrySetResult(true);
				templateOneRoot.Unloaded += (_, _) => templateOneUnloaded.TrySetResult(true);
				return templateOneRoot;
			});

			var templateTwo = new ControlTemplate(() =>
			{
				var child = new Label
				{
					AutomationId = "TemplateTwoContent",
					Text = "Template 2 content"
				};
				templateTwoRoot = new ContentView
				{
					AutomationId = "TemplateTwoRoot",
					Content = child
				};
				templateTwoRoot.Loaded += (_, _) => templateTwoLoaded.TrySetResult(true);
				return templateTwoRoot;
			});

			var templateHost = new ContentView
			{
				AutomationId = "TemplateHost",
				ControlTemplate = templateOne
			};
			var toggleButton = new Button
			{
				AutomationId = "ToggleTemplateButton",
				Text = "Toggle template"
			};
			toggleButton.Clicked += (_, _) =>
			{
				templateHost.ControlTemplate = templateTwo;
				toggleClicked.TrySetResult(true);
			};

			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 16,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			grid.Add(new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 22,
				Text = "ContentView ControlTemplate handler lifecycle"
			});
			grid.Add(templateHost, row: 1);
			grid.Add(toggleButton, row: 2);
			grid.Add(new Label
			{
				Text = "Template replacement lifecycle"
			}, row: 3);
			grid.Add(new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = "Handler cleanup",
				VerticalOptions = LayoutOptions.Start
			}, row: 4);

			var page = new ContentPage
			{
				Title = "ContentView handler lifecycle",
				Content = grid
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await templateOneLoaded.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.NotNull(templateOneRoot);
				Assert.NotNull(templateOneChild);
				Assert.Same(templateOneChild, templateOneRoot.Content);
				Assert.NotNull(templateOneRoot.Handler);
				Assert.NotNull(templateOneRoot.Handler.PlatformView);
				Assert.NotNull(templateOneChild.Handler);
				Assert.NotNull(templateOneChild.Handler.PlatformView);

				await InvokeOnMainThreadAsync(() =>
				{
					var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
					var peer = new WButtonAutomationPeer(buttonHandler.PlatformView);
					var invokeProvider = Assert.IsAssignableFrom<WIInvokeProvider>(peer.GetPattern(WPatternInterface.Invoke));
					invokeProvider.Invoke();
				});

				await toggleClicked.Task.WaitAsync(TimeSpan.FromSeconds(2));
				await templateOneUnloaded.Task.WaitAsync(TimeSpan.FromSeconds(2));
				await templateTwoLoaded.Task.WaitAsync(TimeSpan.FromSeconds(2));

				Assert.Same(templateTwo, templateHost.ControlTemplate);
				Assert.NotNull(templateTwoRoot);
				Assert.NotNull(templateTwoRoot.Handler);
				Assert.NotSame(templateOneRoot, templateTwoRoot);

				await page.Dispatcher.DispatchAsync(() => { });

				bool rootHandlerAttached = templateOneRoot.Handler is not null;
				bool childHandlerAttached = templateOneChild.Handler is not null;
				Assert.True(
					!rootHandlerAttached && !childHandlerAttached,
					$"Unloaded template handlers should be null; root attached: {rootHandlerAttached}; child attached: {childHandlerAttached}.");
			});
		}
	}
}
#endif

