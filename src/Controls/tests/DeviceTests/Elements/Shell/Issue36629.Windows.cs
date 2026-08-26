#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFontFamily = Microsoft.UI.Xaml.Media.FontFamily;
using WFontWeights = Microsoft.UI.Text.FontWeights;
using WTextBox = Microsoft.UI.Xaml.Controls.TextBox;
using WVerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36629 : ControlsHandlerTestBase
	{
		[Category("Issue36629")]
		[Fact]
		public async Task SearchHandlerFontPropertiesUpdateAfterRealization()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Page, PageHandler>();
				});
			});

			const double expectedFontSize = 30;
			const string expectedFontFamily = "Arial";
			const int fontSizeChanged = 1;
			const int fontFamilyChanged = 2;
			const int verticalAlignmentChanged = 4;
			const int fontAttributesChanged = 8;

			var searchHandler = new SearchHandler
			{
				Placeholder = "Search",
				Query = "Styled search text",
				SearchBoxVisibility = SearchBoxVisibility.Expanded
			};

			var fontSizeButton = new Button { Text = "Apply FontSize 30" };
			var fontFamilyButton = new Button { Text = "Apply FontFamily Arial" };
			var verticalAlignmentButton = new Button { Text = "Apply VerticalTextAlignment End" };
			var fontAttributesButton = new Button { Text = "Apply FontAttributes Bold" };
			var propertyChangeMask = -1;

			searchHandler.PropertyChanged += (_, args) =>
			{
				int bit = args.PropertyName switch
				{
					nameof(SearchHandler.FontSize) => fontSizeChanged,
					nameof(SearchHandler.FontFamily) => fontFamilyChanged,
					nameof(SearchHandler.VerticalTextAlignment) => verticalAlignmentChanged,
					nameof(SearchHandler.FontAttributes) => fontAttributesChanged,
					_ => 0
				};

				if (bit != 0)
				{
					if (propertyChangeMask == -1)
						propertyChangeMask = 0;

					propertyChangeMask |= bit;
				}
			};

			fontSizeButton.Clicked += (_, _) => searchHandler.FontSize = expectedFontSize;
			fontFamilyButton.Clicked += (_, _) => searchHandler.FontFamily = expectedFontFamily;
			verticalAlignmentButton.Clicked += (_, _) => searchHandler.VerticalTextAlignment = TextAlignment.End;
			fontAttributesButton.Clicked += (_, _) => searchHandler.FontAttributes = FontAttributes.Bold;

			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label
					{
						Text = "The Shell SearchHandler above starts with platform-default text styling. Apply each reported property and observe the search text.",
						FontSize = 16
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
				Content = new ScrollView { Content = stack }
			};
			var shellContent = new ShellContent
			{
				Title = "SearchHandler styling",
				Content = page,
				Route = "MainPage"
			};
			Shell.SetSearchHandler(shellContent, searchHandler);
			var shell = new Shell { CurrentItem = shellContent };

			await CreateHandlerAndAddToWindow<ShellHandler>(shell, async handler =>
			{
				await OnFrameSetToNotEmpty(page);
				await OnFrameSetToNotEmpty(fontSizeButton);
				await OnFrameSetToNotEmpty(fontFamilyButton);
				await OnFrameSetToNotEmpty(verticalAlignmentButton);
				await OnFrameSetToNotEmpty(fontAttributesButton);
				await AssertEventually(() => handler.PlatformView.ActualWidth > 0 && handler.PlatformView.ActualHeight > 0);

				Assert.NotNull(shell.CurrentItem.Handler);
				var navigationView = shell.CurrentItem.Handler.PlatformView as MauiNavigationView;
				Assert.NotNull(navigationView);
				var autoSuggestBox = navigationView.AutoSuggestBox;
				Assert.NotNull(autoSuggestBox);
				await AssertEventually(() => autoSuggestBox.IsLoaded && autoSuggestBox.ActualWidth > 0);

				var textBox = FindDescendant<WTextBox>(autoSuggestBox);
				Assert.NotNull(textBox);
				await AssertEventually(() => textBox.IsLoaded && textBox.ActualWidth > 0 && textBox.ActualHeight > 0);

				var nativeFontSizeButton = GetNativeButton(fontSizeButton);
				var nativeFontFamilyButton = GetNativeButton(fontFamilyButton);
				var nativeVerticalAlignmentButton = GetNativeButton(verticalAlignmentButton);
				var nativeFontAttributesButton = GetNativeButton(fontAttributesButton);
				Assert.NotNull(nativeFontSizeButton);
				Assert.NotNull(nativeFontFamilyButton);
				Assert.NotNull(nativeVerticalAlignmentButton);
				Assert.NotNull(nativeFontAttributesButton);
				Assert.True(nativeFontSizeButton.IsLoaded && nativeFontSizeButton.ActualWidth > 0);
				Assert.True(nativeFontFamilyButton.IsLoaded && nativeFontFamilyButton.ActualWidth > 0);
				Assert.True(nativeVerticalAlignmentButton.IsLoaded && nativeVerticalAlignmentButton.ActualWidth > 0);
				Assert.True(nativeFontAttributesButton.IsLoaded && nativeFontAttributesButton.ActualWidth > 0);

				double initialFontSize = textBox.FontSize;
				string initialFontFamily = textBox.FontFamily.Source;
				WVerticalAlignment initialVerticalAlignment = textBox.VerticalContentAlignment;
				ushort initialFontWeight = textBox.FontWeight.Weight;
				var arialFontFamily = new WFontFamily(expectedFontFamily);
				Assert.Equal(expectedFontFamily, arialFontFamily.Source);

				Invoke(nativeFontSizeButton);
				await Task.Yield();
				Invoke(nativeFontFamilyButton);
				await Task.Yield();
				Invoke(nativeVerticalAlignmentButton);
				await Task.Yield();
				Invoke(nativeFontAttributesButton);
				await Task.Yield();

				Assert.NotEqual(-1, propertyChangeMask);
				Assert.Equal(
					fontSizeChanged | fontFamilyChanged | verticalAlignmentChanged | fontAttributesChanged,
					propertyChangeMask);
				Assert.Equal(expectedFontSize, searchHandler.FontSize);
				Assert.Equal(expectedFontFamily, searchHandler.FontFamily);
				Assert.Equal(TextAlignment.End, searchHandler.VerticalTextAlignment);
				Assert.Equal(FontAttributes.Bold, searchHandler.FontAttributes);

				bool fontSizeApplied = await WaitForAsync(() => Math.Abs(textBox.FontSize - expectedFontSize) <= 0.01);
				bool fontFamilyApplied = await WaitForAsync(() => textBox.FontFamily.Source == expectedFontFamily);
				bool verticalAlignmentApplied = await WaitForAsync(() => textBox.VerticalContentAlignment == WVerticalAlignment.Bottom);
				bool fontAttributesApplied = await WaitForAsync(() => textBox.FontWeight.Weight == WFontWeights.Bold.Weight);

				Assert.True(fontSizeApplied,
					$"SearchHandler FontSize was not applied to the native TextBox. Initial: {initialFontSize}; expected: {expectedFontSize}; actual: {textBox.FontSize}.");
				Assert.True(fontFamilyApplied,
					$"SearchHandler FontFamily was not applied to the native TextBox. Initial: {initialFontFamily}; expected: {expectedFontFamily}; actual: {textBox.FontFamily.Source}.");
				Assert.True(verticalAlignmentApplied,
					$"SearchHandler VerticalTextAlignment was not applied to the native TextBox. Initial: {initialVerticalAlignment}; expected: {WVerticalAlignment.Bottom}; actual: {textBox.VerticalContentAlignment}.");
				Assert.True(fontAttributesApplied,
					$"SearchHandler FontAttributes was not applied to the native TextBox. Initial weight: {initialFontWeight}; expected: {WFontWeights.Bold.Weight}; actual: {textBox.FontWeight.Weight}.");
			});
		}

		static WButton GetNativeButton(Button button) =>
			button.Handler.PlatformView as WButton;

		static void Invoke(WButton button)
		{
			var peer = new ButtonAutomationPeer(button);
			var invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
			Assert.NotNull(invokeProvider);
			invokeProvider.Invoke();
		}

		static T FindDescendant<T>(WDependencyObject parent)
			where T : WDependencyObject
		{
			int childCount = WVisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childCount; i++)
			{
				var child = WVisualTreeHelper.GetChild(parent, i);
				if (child is T match)
					return match;

				var descendant = FindDescendant<T>(child);
				if (descendant is not null)
					return descendant;
			}

			return null;
		}

		static async Task<bool> WaitForAsync(Func<bool> predicate)
		{
			for (int i = 0; i < 100; i++)
			{
				if (predicate())
					return true;

				await Task.Yield();
			}

			return predicate();
		}
	}
}
#endif

