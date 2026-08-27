using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WButtonAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer;
using WColor = Windows.UI.Color;
using WIInvokeProvider = Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider;
using WPatternInterface = Microsoft.UI.Xaml.Automation.Peers.PatternInterface;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue37540")]
public class Issue37540 : ControlsHandlerTestBase
{
	const string FailureSignature = "Issue37540 native Label background after the reloaded Loaded event should match the red resource";

	[Fact]
	public async Task DynamicResourceUpdatesBackgroundAfterLabelIsReloaded()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Grid, LayoutHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<Label, LabelHandler>();
			});
		});

		var resourceColor = Colors.Red;
		var affectedLabel = new Label
		{
			AutomationId = "AffectedLabel",
			Background = new SolidColorBrush(Colors.Transparent),
			HeightRequest = 120,
			HorizontalTextAlignment = TextAlignment.Center,
			Text = "Affected label: background should turn Red",
			VerticalTextAlignment = TextAlignment.Center
		};
		var reproHost = new Grid { affectedLabel };
		var referenceLabel = new Label
		{
			Background = new SolidColorBrush(resourceColor),
			HeightRequest = 72,
			HorizontalTextAlignment = TextAlignment.Center,
			Text = "Expected background: Red",
			TextColor = Colors.White,
			VerticalTextAlignment = TextAlignment.Center
		};
		var triggerButton = new Button
		{
			AutomationId = "TriggerButton",
			Text = "Reload label and apply resource"
		};
		var contentStack = new VerticalStackLayout
		{
			Spacing = 12,
			Children =
			{
				referenceLabel,
				reproHost
			}
		};
		var root = new Grid
		{
			Padding = 24,
			RowSpacing = 16,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		var titleLabel = new Label
		{
			FontAttributes = FontAttributes.Bold,
			FontSize = 22,
			Text = "SetDynamicResource on Label.Background"
		};
		var descriptionLabel = new Label
		{
			Text = "The affected label starts transparent. After it loads again, its background should match the red reference below."
		};
		root.Add(titleLabel);
		root.Add(descriptionLabel);
		root.Add(triggerButton);
		root.Add(contentStack);
		Grid.SetRow(descriptionLabel, 1);
		Grid.SetRow(triggerButton, 2);
		Grid.SetRow(contentStack, 3);

		var page = new ContentPage { Content = root };
		page.Resources["backgroundColor"] = resourceColor;

		var triggerArmed = false;
		var removalObserved = false;
		var readdDispatched = false;
		var loadCount = 0;
		var postTriggerLoadObservation = -1;
		var postTriggerLoaded = new TaskCompletionSource<int>();

		affectedLabel.Loaded += (_, _) =>
		{
			loadCount++;
			if (!triggerArmed)
				return;

			triggerArmed = false;
			affectedLabel.SetDynamicResource(Label.BackgroundProperty, "backgroundColor");
			postTriggerLoadObservation = loadCount;
			postTriggerLoaded.TrySetResult(loadCount);
		};

		triggerButton.Clicked += (_, _) =>
		{
			triggerArmed = true;
			reproHost.Remove(affectedLabel);
			removalObserved = !reproHost.Children.Contains(affectedLabel);
			readdDispatched = reproHost.Dispatcher.Dispatch(() => reproHost.Add(affectedLabel));
		};

		await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
		{
			Assert.True(loadCount > 0);
			Assert.Contains(affectedLabel, reproHost.Children);

			var referenceHandler = Assert.IsType<LabelHandler>(referenceLabel.Handler);
			var affectedHandler = Assert.IsType<LabelHandler>(affectedLabel.Handler);
			var buttonHandler = Assert.IsType<ButtonHandler>(triggerButton.Handler);
			var referenceContainer = Assert.IsType<WrapperView>(referenceHandler.ContainerView);
			var affectedContainer = Assert.IsType<WrapperView>(affectedHandler.ContainerView);
			var referenceBrush = Assert.IsType<WSolidColorBrush>(referenceContainer.Background);
			var initialAffectedBrush = Assert.IsType<WSolidColorBrush>(affectedContainer.Background);
			var expectedResourceColor = Assert.IsType<Color>(page.Resources["backgroundColor"]);
			var expectedColor = expectedResourceColor.ToWindowsColor();

			Assert.Equal(expectedColor, referenceBrush.Color);
			Assert.Equal(0, initialAffectedBrush.Color.A);
			Assert.True(referenceHandler.PlatformView.ActualWidth > 0);
			Assert.True(referenceHandler.PlatformView.ActualHeight > 0);
			Assert.True(affectedHandler.PlatformView.ActualWidth > 0);
			Assert.True(affectedHandler.PlatformView.ActualHeight > 0);

			var automationPeer = new WButtonAutomationPeer(buttonHandler.PlatformView);
			var invokeProvider = automationPeer.GetPattern(WPatternInterface.Invoke) as WIInvokeProvider;
			Assert.NotNull(invokeProvider);
			invokeProvider.Invoke();

			Assert.True(removalObserved);
			Assert.True(readdDispatched);
			await postTriggerLoaded.Task.WaitAsync(TimeSpan.FromSeconds(2));
			Assert.NotEqual(-1, postTriggerLoadObservation);
			Assert.True(postTriggerLoadObservation > 1);
			Assert.Contains(affectedLabel, reproHost.Children);
			Assert.Same(reproHost, affectedLabel.Parent);
			Assert.Equal(expectedResourceColor, Assert.IsType<Color>(page.Resources["backgroundColor"]));
			var reloadedHandler = Assert.IsType<LabelHandler>(affectedLabel.Handler);
			Assert.True(reloadedHandler.PlatformView.ActualWidth > 0);
			Assert.True(reloadedHandler.PlatformView.ActualHeight > 0);

			WColor actualColor = default;
			await AssertEventually(
				() =>
				{
					if (reloadedHandler.ContainerView is not WrapperView container ||
						container.Background is not WSolidColorBrush brush)
						return false;

					actualColor = brush.Color;
					return actualColor.Equals(expectedColor);
				},
				message: $"{FailureSignature}. Expected ARGB {FormatArgb(expectedColor)}.");

			Assert.True(
				actualColor.Equals(expectedColor),
				$"{FailureSignature}. Expected ARGB {FormatArgb(expectedColor)}, actual ARGB {FormatArgb(actualColor)}.");
		});
	}

	static string FormatArgb(WColor color) =>
		$"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}

