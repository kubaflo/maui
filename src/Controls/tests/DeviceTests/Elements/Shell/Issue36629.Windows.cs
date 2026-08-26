#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WAutoSuggestBox = Microsoft.UI.Xaml.Controls.AutoSuggestBox;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WVerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36629")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36629 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RuntimeFontPropertiesPropagateToNativeSearchBox()
		{
			EnsureHandlerCreated(builder => builder.SetupShellHandlers());

			const double requestedFontSize = 30;
			const string requestedFontFamily = "Arial";

			var fontSizeApplied = false;
			var fontFamilyApplied = false;
			var verticalAlignmentApplied = false;
			var fontAttributesApplied = false;
			var lastAction = -1;

			var searchHandler = new SearchHandler
			{
				Query = "Search text",
				Placeholder = "Search text",
				ShowsResults = false,
			};

			var actionStatusLabel = new Label { Text = "Applied: 0/4" };
			var observedStyleLabel = new Label { Text = "Observed native style: not checked" };

			var fontSizeButton = new Button { Text = "Apply FontSize" };
			fontSizeButton.Clicked += (_, _) =>
			{
				searchHandler.FontSize = requestedFontSize;
				fontSizeApplied = true;
				lastAction = 0;
				actionStatusLabel.Text = "Applied: 1/4";
			};

			var fontFamilyButton = new Button { Text = "Apply FontFamily" };
			fontFamilyButton.Clicked += (_, _) =>
			{
				searchHandler.FontFamily = requestedFontFamily;
				fontFamilyApplied = true;
				lastAction = 1;
				actionStatusLabel.Text = "Applied: 2/4";
			};

			var verticalAlignmentButton = new Button { Text = "Apply VerticalTextAlignment" };
			verticalAlignmentButton.Clicked += (_, _) =>
			{
				searchHandler.VerticalTextAlignment = TextAlignment.End;
				verticalAlignmentApplied = true;
				lastAction = 2;
				actionStatusLabel.Text = "Applied: 3/4";
			};

			var fontAttributesButton = new Button { Text = "Apply FontAttributes" };
			fontAttributesButton.Clicked += (_, _) =>
			{
				searchHandler.FontAttributes = FontAttributes.Bold;
				fontAttributesApplied = true;
				lastAction = 3;
				actionStatusLabel.Text = "Applied: 4/4";
			};

			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 10,
				Children =
				{
					new Label { Text = "Change each SearchHandler text style, then check the rendered Windows search box." },
					fontSizeButton,
					fontFamilyButton,
					verticalAlignmentButton,
					fontAttributesButton,
					actionStatusLabel,
					observedStyleLabel,
				},
			};

			var page = new ContentPage
			{
				Content = new ScrollView { Content = content },
			};
			Shell.SetSearchHandler(page, searchHandler);

			var shell = new Shell
			{
				Items =
				{
					new ShellContent
					{
						Title = "SearchHandler styles",
						Content = page,
					},
				},
			};

			await CreateHandlerAndAddToWindow<ShellHandler>(shell, async _ =>
			{
				await OnLoadedAsync(page);

				WAutoSuggestBox nativeSearchBox = null;
				await AssertEventually(
					() =>
					{
						var navigationView = shell.CurrentItem.Handler.PlatformView as MauiNavigationView;
						nativeSearchBox = navigationView?.AutoSuggestBox;
						return nativeSearchBox is not null && nativeSearchBox.IsLoaded;
					},
					message: "The native AutoSuggestBox was not loaded");

				var initialFontSize = nativeSearchBox.FontSize;
				var initialFontFamily = nativeSearchBox.FontFamily?.Source;
				var initialVerticalAlignment = nativeSearchBox.VerticalContentAlignment;
				var initialFontWeight = nativeSearchBox.FontWeight.Weight;

				Assert.NotEqual(requestedFontSize, initialFontSize);
				Assert.False(string.Equals(requestedFontFamily, initialFontFamily, StringComparison.OrdinalIgnoreCase));
				Assert.NotEqual(WVerticalAlignment.Bottom, initialVerticalAlignment);
				Assert.NotEqual(Microsoft.UI.Text.FontWeights.Bold.Weight, initialFontWeight);

				InvokeButton(fontSizeButton);
				InvokeButton(fontFamilyButton);
				InvokeButton(verticalAlignmentButton);
				InvokeButton(fontAttributesButton);

				Assert.True(fontSizeApplied);
				Assert.True(fontFamilyApplied);
				Assert.True(verticalAlignmentApplied);
				Assert.True(fontAttributesApplied);
				Assert.Equal(3, lastAction);

				Assert.Equal(requestedFontSize, searchHandler.FontSize);
				Assert.Equal(requestedFontFamily, searchHandler.FontFamily);
				Assert.Equal(TextAlignment.End, searchHandler.VerticalTextAlignment);
				Assert.Equal(FontAttributes.Bold, searchHandler.FontAttributes);

				observedStyleLabel.Text =
					$"Observed native style: FontSize={nativeSearchBox.FontSize:0.##}, FontFamily={nativeSearchBox.FontFamily?.Source}, " +
					$"Vertical={nativeSearchBox.VerticalContentAlignment}, Weight={nativeSearchBox.FontWeight.Weight}";

				await AssertEventually(
					() => Math.Abs(nativeSearchBox.FontSize - requestedFontSize) <= 0.01,
					message: $"SearchHandler FontSize was not propagated to the native AutoSuggestBox. Expected {requestedFontSize}; observed {nativeSearchBox.FontSize}.");
				await AssertEventually(
					() => string.Equals(nativeSearchBox.FontFamily?.Source, requestedFontFamily, StringComparison.OrdinalIgnoreCase),
					message: $"SearchHandler FontFamily was not propagated to the native AutoSuggestBox. Expected {requestedFontFamily}; observed {nativeSearchBox.FontFamily?.Source}.");
				await AssertEventually(
					() => nativeSearchBox.VerticalContentAlignment == WVerticalAlignment.Bottom,
					message: $"SearchHandler VerticalTextAlignment was not propagated to the native AutoSuggestBox. Expected {WVerticalAlignment.Bottom}; observed {nativeSearchBox.VerticalContentAlignment}.");
				await AssertEventually(
					() => nativeSearchBox.FontWeight.Weight == Microsoft.UI.Text.FontWeights.Bold.Weight,
					message: $"SearchHandler FontAttributes was not propagated to the native AutoSuggestBox. Expected {Microsoft.UI.Text.FontWeights.Bold.Weight}; observed {nativeSearchBox.FontWeight.Weight}.");
			});
		}

		static void InvokeButton(Button button)
		{
			var platformButton = button.Handler.PlatformView as WButton;
			Assert.NotNull(platformButton);

			var automationPeer = new ButtonAutomationPeer(platformButton);
			var invokeProvider = automationPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
			Assert.NotNull(invokeProvider);
			invokeProvider.Invoke();
		}
	}
}
#endif

