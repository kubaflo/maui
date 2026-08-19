#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36629")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36629 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task SearchHandlerStylingUpdatesNativeSearchBoxAfterAttachment()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.SetupShellHandlers();
					handlers.AddHandler<Window, WindowHandlerStub>();
				});
			});

			var searchHandler = new SearchHandler
			{
				AutomationId = "AffectedSearchHandler",
				Placeholder = "Search text",
				Query = "Search text",
				SearchBoxVisibility = SearchBoxVisibility.Expanded
			};
			var referenceLabel = new Label
			{
				AutomationId = "ExpectedStyle",
				Text = "Expected style: Search text",
				Padding = 10,
				BackgroundColor = Colors.LightGray
			};
			var fontSizeButton = new Button { AutomationId = "FontSizeButton", Text = "Apply FontSize" };
			var fontFamilyButton = new Button { AutomationId = "FontFamilyButton", Text = "Apply FontFamily" };
			var verticalAlignmentButton = new Button { AutomationId = "VerticalTextAlignmentButton", Text = "Apply VerticalTextAlignment" };
			var fontAttributesButton = new Button { AutomationId = "FontAttributesButton", Text = "Apply FontAttributes" };
			var resultLabel = new Label
			{
				AutomationId = "ResultLabel",
				Text = "NO BUG: SearchHandler styling has not failed",
				FontAttributes = FontAttributes.Bold,
				FontSize = 18
			};
			var callbackSequence = -1;
			var fontSizeApplied = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
			var fontFamilyApplied = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
			var verticalAlignmentApplied = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
			var fontAttributesApplied = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

			fontSizeButton.Clicked += (_, _) =>
			{
				searchHandler.FontSize = 28;
				referenceLabel.FontSize = 28;
				fontSizeApplied.SetResult(++callbackSequence);
			};
			fontFamilyButton.Clicked += (_, _) =>
			{
				searchHandler.FontFamily = "Arial";
				referenceLabel.FontFamily = "Arial";
				fontFamilyApplied.SetResult(++callbackSequence);
			};
			verticalAlignmentButton.Clicked += (_, _) =>
			{
				searchHandler.VerticalTextAlignment = TextAlignment.End;
				referenceLabel.VerticalTextAlignment = TextAlignment.End;
				verticalAlignmentApplied.SetResult(++callbackSequence);
			};
			fontAttributesButton.Clicked += (_, _) =>
			{
				searchHandler.FontAttributes = FontAttributes.Bold;
				referenceLabel.FontAttributes = FontAttributes.Bold;
				fontAttributesApplied.SetResult(++callbackSequence);
			};

			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label
					{
						Text = "The search box above should match the reference text as each style is applied.",
						FontSize = 18
					},
					referenceLabel,
					fontSizeButton,
					fontFamilyButton,
					verticalAlignmentButton,
					fontAttributesButton,
					resultLabel
				}
			};
			var page = new ContentPage
			{
				Title = "SearchHandler styling",
				Content = new ScrollView { Content = content }
			};
			Shell.SetSearchHandler(page, searchHandler);

			var shell = new Shell
			{
				FlyoutBehavior = FlyoutBehavior.Disabled,
				Items =
				{
					new ShellContent
					{
						Title = "SearchHandler styling",
						Content = page
					}
				}
			};

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(shell), async _ =>
			{
				await OnLoadedAsync(page);
				await OnFrameSetToNotEmpty(page);

				var navigationView = Assert.IsAssignableFrom<Microsoft.UI.Xaml.Controls.NavigationView>(shell.CurrentItem.Handler.PlatformView);
				var searchBox = navigationView.AutoSuggestBox;
				Assert.NotNull(searchBox);

				var initialNativeFontSize = searchBox.FontSize;
				var initialNativeFontFamily = searchBox.FontFamily.Source;
				var initialNativeVerticalAlignment = searchBox.VerticalContentAlignment;
				var initialNativeFontWeight = searchBox.FontWeight;

				Assert.NotEqual(28d, searchHandler.FontSize);
				Assert.NotEqual(28d, referenceLabel.FontSize);
				Assert.Null(searchHandler.FontFamily);
				Assert.Null(referenceLabel.FontFamily);
				Assert.Equal(TextAlignment.Center, searchHandler.VerticalTextAlignment);
				Assert.Equal(TextAlignment.Start, referenceLabel.VerticalTextAlignment);
				Assert.Equal(FontAttributes.None, searchHandler.FontAttributes);
				Assert.Equal(FontAttributes.None, referenceLabel.FontAttributes);

				InvokeNativeButton(fontSizeButton);
				Assert.Equal(0, await fontSizeApplied.Task.WaitAsync(TimeSpan.FromSeconds(2)));
				Assert.Equal(28d, searchHandler.FontSize);
				Assert.Equal(28d, referenceLabel.FontSize);

				InvokeNativeButton(fontFamilyButton);
				Assert.Equal(1, await fontFamilyApplied.Task.WaitAsync(TimeSpan.FromSeconds(2)));
				Assert.Equal("Arial", searchHandler.FontFamily);
				Assert.Equal("Arial", referenceLabel.FontFamily);

				InvokeNativeButton(verticalAlignmentButton);
				Assert.Equal(2, await verticalAlignmentApplied.Task.WaitAsync(TimeSpan.FromSeconds(2)));
				Assert.Equal(TextAlignment.End, searchHandler.VerticalTextAlignment);
				Assert.Equal(TextAlignment.End, referenceLabel.VerticalTextAlignment);

				InvokeNativeButton(fontAttributesButton);
				Assert.Equal(3, await fontAttributesApplied.Task.WaitAsync(TimeSpan.FromSeconds(2)));
				Assert.Equal(FontAttributes.Bold, searchHandler.FontAttributes);
				Assert.Equal(FontAttributes.Bold, referenceLabel.FontAttributes);

				double settledFontSize = searchBox.FontSize;
				string settledFontFamily = searchBox.FontFamily.Source;
				var settledVerticalAlignment = searchBox.VerticalContentAlignment;
				var settledFontWeight = searchBox.FontWeight;

				await AssertEventually(
					() => Math.Abs((settledFontSize = searchBox.FontSize) - 28) <= 0.01,
					message: $"SearchHandler FontSize native value was {settledFontSize}; expected 28 after starting at {initialNativeFontSize}.");
				await AssertEventually(
					() => string.Equals(
						settledFontFamily = searchBox.FontFamily.Source,
						"Arial",
						StringComparison.OrdinalIgnoreCase),
					message: $"SearchHandler FontFamily native value was {settledFontFamily}; expected Arial.");
				await AssertEventually(
					() => (settledVerticalAlignment = searchBox.VerticalContentAlignment) == Microsoft.UI.Xaml.VerticalAlignment.Bottom,
					message: $"SearchHandler VerticalContentAlignment native value was {settledVerticalAlignment}; expected Bottom.");
				await AssertEventually(
					() => (settledFontWeight = searchBox.FontWeight).Weight == Microsoft.UI.Text.FontWeights.Bold.Weight,
					message: $"SearchHandler FontWeight native value was {settledFontWeight.Weight}; expected {Microsoft.UI.Text.FontWeights.Bold.Weight}.");

				Assert.True(Math.Abs(settledFontSize - 28) <= 0.01, $"SearchHandler FontSize native value was {settledFontSize}; expected 28.");
				Assert.True(string.Equals(settledFontFamily, "Arial", StringComparison.OrdinalIgnoreCase), $"SearchHandler FontFamily native value was {settledFontFamily}; expected Arial after starting at {initialNativeFontFamily}.");
				Assert.True(settledVerticalAlignment == Microsoft.UI.Xaml.VerticalAlignment.Bottom, $"SearchHandler VerticalContentAlignment native value was {settledVerticalAlignment}; expected Bottom after starting at {initialNativeVerticalAlignment}.");
				Assert.True(settledFontWeight.Weight == Microsoft.UI.Text.FontWeights.Bold.Weight, $"SearchHandler FontWeight native value was {settledFontWeight.Weight}; expected {Microsoft.UI.Text.FontWeights.Bold.Weight} after starting at {initialNativeFontWeight.Weight}.");
			});
		}

		static void InvokeNativeButton(Button button)
		{
			var handler = Assert.IsAssignableFrom<ButtonHandler>(button.Handler);
			var nativeButton = Assert.IsAssignableFrom<Microsoft.UI.Xaml.Controls.Button>(handler.PlatformView);
			var peer = new ButtonAutomationPeer(nativeButton);
			var invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke));
			invokeProvider.Invoke();
		}
	}
}
#endif
