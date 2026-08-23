#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WAutoSuggestBox = Microsoft.UI.Xaml.Controls.AutoSuggestBox;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WButtonAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer;
using WFontStyle = Windows.UI.Text.FontStyle;
using WFontWeights = Microsoft.UI.Text.FontWeights;
using WIInvokeProvider = Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider;
using WPatternInterface = Microsoft.UI.Xaml.Automation.Peers.PatternInterface;
using WVerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36629")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36629 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task SearchHandlerRuntimeFontPropertiesUpdateNativeSearchBox()
		{
			EnsureHandlerCreated(builder => builder.SetupShellHandlers());

			var searchHandler = new SearchHandler
			{
				Placeholder = "Search text",
				Query = "Search styling"
			};

			var fontSizeButton = new Button { Text = "Apply FontSize = 32" };
			var fontFamilyButton = new Button { Text = "Apply FontFamily = Courier New" };
			var verticalAlignmentButton = new Button { Text = "Apply VerticalTextAlignment = End" };
			var fontAttributesButton = new Button { Text = "Apply FontAttributes = Bold, Italic" };

			bool fontSizeChanged = false;
			bool fontFamilyChanged = false;
			bool verticalAlignmentChanged = false;
			bool fontAttributesChanged = false;

			searchHandler.PropertyChanged += (_, args) =>
			{
				switch (args.PropertyName)
				{
					case nameof(SearchHandler.FontSize):
						fontSizeChanged = true;
						break;
					case nameof(SearchHandler.FontFamily):
						fontFamilyChanged = true;
						break;
					case nameof(SearchHandler.VerticalTextAlignment):
						verticalAlignmentChanged = true;
						break;
					case nameof(SearchHandler.FontAttributes):
						fontAttributesChanged = true;
						break;
				}
			};

			fontSizeButton.Clicked += (_, _) =>
			{
				searchHandler.FontSize = 32;
			};
			fontFamilyButton.Clicked += (_, _) =>
			{
				searchHandler.FontFamily = "Courier New";
			};
			verticalAlignmentButton.Clicked += (_, _) =>
			{
				searchHandler.VerticalTextAlignment = TextAlignment.End;
			};
			fontAttributesButton.Clicked += (_, _) =>
			{
				searchHandler.FontAttributes = FontAttributes.Bold | FontAttributes.Italic;
			};

			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 10,
				Children =
				{
					new Label
					{
						Text = "Windows SearchHandler runtime styling",
						FontAttributes = FontAttributes.Bold,
						FontSize = 22
					},
					new Label
					{
						Text = "The SearchHandler above starts with platform-default styling. Apply the four reported properties in order."
					},
					fontSizeButton,
					fontFamilyButton,
					verticalAlignmentButton,
					fontAttributesButton
				}
			};
			var page = new ContentPage
			{
				Title = "SearchHandler styling",
				Content = new ScrollView { Content = content }
			};
			Shell.SetSearchHandler(page, searchHandler);

			var shell = new Shell { CurrentItem = page };

			await CreateHandlerAndAddToWindow(shell, async () =>
			{
				await OnLoadedAsync(page);
				await AssertEventually(
					() => shell.CurrentItem.Handler?.PlatformView is MauiNavigationView navigationView &&
						navigationView.AutoSuggestBox?.IsLoaded == true,
					message: "The Shell SearchHandler native AutoSuggestBox did not load.");

				var shellItemHandler = Assert.IsType<ShellItemHandler>(shell.CurrentItem.Handler);
				var navigationView = Assert.IsType<MauiNavigationView>(shellItemHandler.PlatformView);
				var searchBox = Assert.IsType<WAutoSuggestBox>(navigationView.AutoSuggestBox);

				Assert.Equal("Search styling", searchHandler.Query);
				Assert.Equal("Search text", searchHandler.Placeholder);
				Assert.Equal("Search styling", searchBox.Text);
				Assert.Equal("Search text", searchBox.PlaceholderText);
				Assert.True(Math.Abs(searchBox.FontSize - 32) >= 0.01);
				Assert.DoesNotContain("Courier New", searchBox.FontFamily.Source, StringComparison.OrdinalIgnoreCase);
				Assert.NotEqual(WVerticalAlignment.Bottom, searchBox.VerticalContentAlignment);
				Assert.NotEqual(WFontWeights.Bold.Weight, searchBox.FontWeight.Weight);
				Assert.NotEqual(WFontStyle.Italic, searchBox.FontStyle);

				await InvokeButton(fontSizeButton);
				await AssertEventually(() => fontSizeChanged, message: "SearchHandler.FontSize did not raise PropertyChanged after the button click.");
				AssertNativeSearchBoxUnchanged(shell, navigationView, searchBox);

				await InvokeButton(fontFamilyButton);
				await AssertEventually(() => fontFamilyChanged, message: "SearchHandler.FontFamily did not raise PropertyChanged after the button click.");
				AssertNativeSearchBoxUnchanged(shell, navigationView, searchBox);

				await InvokeButton(verticalAlignmentButton);
				await AssertEventually(() => verticalAlignmentChanged, message: "SearchHandler.VerticalTextAlignment did not raise PropertyChanged after the button click.");
				AssertNativeSearchBoxUnchanged(shell, navigationView, searchBox);

				await InvokeButton(fontAttributesButton);
				await AssertEventually(() => fontAttributesChanged, message: "SearchHandler.FontAttributes did not raise PropertyChanged after the button click.");
				AssertNativeSearchBoxUnchanged(shell, navigationView, searchBox);

				await AssertEventually(
					() => Math.Abs(searchBox.FontSize - 32) < 0.01,
					message: $"SearchHandler FontSize native value was {searchBox.FontSize:0.##}; expected 32 after the runtime button click.");
				await AssertEventually(
					() => searchBox.FontFamily.Source.Contains("Courier New", StringComparison.OrdinalIgnoreCase),
					message: $"SearchHandler FontFamily native value was {searchBox.FontFamily.Source}; expected Courier New after the runtime button click.");
				await AssertEventually(
					() => searchBox.VerticalContentAlignment == WVerticalAlignment.Bottom,
					message: $"SearchHandler VerticalTextAlignment native value was {searchBox.VerticalContentAlignment}; expected Bottom after the runtime button click.");
				await AssertEventually(
					() => searchBox.FontWeight.Weight == WFontWeights.Bold.Weight,
					message: $"SearchHandler FontAttributes native weight was {searchBox.FontWeight.Weight}; expected {WFontWeights.Bold.Weight} after the runtime button click.");
				await AssertEventually(
					() => searchBox.FontStyle == WFontStyle.Italic,
					message: $"SearchHandler FontAttributes native style was {searchBox.FontStyle}; expected Italic after the runtime button click.");
			});
		}

		Task InvokeButton(Button button)
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var buttonHandler = Assert.IsType<ButtonHandler>(button.Handler);
				var platformButton = Assert.IsAssignableFrom<WButton>(buttonHandler.PlatformView);
				var automationPeer = new WButtonAutomationPeer(platformButton);
				var invokeProvider = Assert.IsAssignableFrom<WIInvokeProvider>(
					automationPeer.GetPattern(WPatternInterface.Invoke));
				invokeProvider.Invoke();
			});
		}

		static void AssertNativeSearchBoxUnchanged(
			Shell shell,
			MauiNavigationView expectedNavigationView,
			WAutoSuggestBox expectedSearchBox)
		{
			var shellItemHandler = Assert.IsType<ShellItemHandler>(shell.CurrentItem.Handler);
			var currentNavigationView = Assert.IsType<MauiNavigationView>(shellItemHandler.PlatformView);

			Assert.Same(expectedNavigationView, currentNavigationView);
			Assert.Same(expectedSearchBox, currentNavigationView.AutoSuggestBox);
			Assert.True(expectedSearchBox.IsLoaded);
		}
	}
}
#endif

