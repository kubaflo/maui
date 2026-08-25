#if WINDOWS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WAutoSuggestBox = Microsoft.UI.Xaml.Controls.AutoSuggestBox;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFontWeights = Microsoft.UI.Text.FontWeights;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WNavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WTextBox = Microsoft.UI.Xaml.Controls.TextBox;
using WVerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36629")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36629 : ControlsHandlerTestBase
	{
		const string FailureSignature = "SearchHandler runtime styling mismatch after all four button actions:";

		[Fact]
		public async Task RuntimeFontPropertiesUpdateNativeSearchTextBox()
		{
			const double expectedFontSize = 32;
			const string expectedFontFamily = "Courier New";
			const FontAttributes expectedFontAttributes = FontAttributes.Bold;
			const TextAlignment expectedVerticalTextAlignment = TextAlignment.End;

			var searchHandler = new SearchHandler
			{
				Placeholder = "SearchHandler should match the reference",
				Query = "Expected styled search text",
				SearchBoxVisibility = SearchBoxVisibility.Expanded
			};

			var referenceLabel = new Label
			{
				Text = "Expected styled search text",
				FontSize = expectedFontSize,
				FontFamily = expectedFontFamily,
				FontAttributes = expectedFontAttributes,
				VerticalTextAlignment = expectedVerticalTextAlignment
			};

			var fontSizeButton = new Button { Text = "FontSize" };
			var fontFamilyButton = new Button { Text = "FontFamily" };
			var verticalTextAlignmentButton = new Button { Text = "VerticalTextAlignment" };
			var fontAttributesButton = new Button { Text = "FontAttributes" };
			var clickedProperties = new List<string>();
			var changedProperties = new List<string>();
			int postActionState = -1;

			fontSizeButton.Clicked += (_, _) =>
			{
				clickedProperties.Add(nameof(SearchHandler.FontSize));
				searchHandler.FontSize = expectedFontSize;
			};
			fontFamilyButton.Clicked += (_, _) =>
			{
				clickedProperties.Add(nameof(SearchHandler.FontFamily));
				searchHandler.FontFamily = expectedFontFamily;
			};
			verticalTextAlignmentButton.Clicked += (_, _) =>
			{
				clickedProperties.Add(nameof(SearchHandler.VerticalTextAlignment));
				searchHandler.VerticalTextAlignment = expectedVerticalTextAlignment;
			};
			fontAttributesButton.Clicked += (_, _) =>
			{
				clickedProperties.Add(nameof(SearchHandler.FontAttributes));
				searchHandler.FontAttributes = expectedFontAttributes;
			};
			searchHandler.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName is nameof(SearchHandler.FontSize)
					or nameof(SearchHandler.FontFamily)
					or nameof(SearchHandler.VerticalTextAlignment)
					or nameof(SearchHandler.FontAttributes))
				{
					changedProperties.Add(args.PropertyName);
				}
			};

			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Border
					{
						Stroke = Microsoft.Maui.Graphics.Colors.Gray,
						StrokeThickness = 1,
						HeightRequest = 64,
						Content = referenceLabel
					},
					fontSizeButton,
					fontFamilyButton,
					verticalTextAlignmentButton,
					fontAttributesButton
				}
			};
			var page = new ContentPage
			{
				Content = new ScrollView { Content = stack }
			};
			Shell.SetSearchHandler(page, searchHandler);

			var shellContent = new ShellContent { Content = page };
			var shell = new Shell
			{
				FlyoutBehavior = FlyoutBehavior.Disabled,
				Items = { shellContent }
			};
			var testWindow = new Microsoft.Maui.Controls.Window(shell);

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.SetupShellHandlers();
					handlers.AddHandler(typeof(Microsoft.Maui.Controls.Window), typeof(WindowHandlerStub));
					handlers.AddHandler(typeof(Page), typeof(PageHandler));
					handlers.AddHandler(typeof(Layout), typeof(LayoutHandler));
					handlers.AddHandler(typeof(Border), typeof(BorderHandler));
					handlers.AddHandler(typeof(Label), typeof(LabelHandler));
					handlers.AddHandler(typeof(Button), typeof(ButtonHandler));
					handlers.AddHandler(typeof(ScrollView), typeof(ScrollViewHandler));
				});
			});

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(testWindow, async _ =>
			{
				await AssertEventually(
					() => IsLoaded(page)
						&& IsLoaded(referenceLabel)
						&& IsLoaded(fontSizeButton)
						&& IsLoaded(fontFamilyButton)
						&& IsLoaded(verticalTextAlignmentButton)
						&& IsLoaded(fontAttributesButton)
						&& shell.CurrentItem?.Handler?.PlatformView is WNavigationView,
					message: "The recorded SearchHandler page and controls did not load.");

				var nativeReference = referenceLabel.Handler.PlatformView as WTextBlock;
				Assert.NotNull(nativeReference);
				Assert.Equal(expectedFontSize, nativeReference.FontSize, 2);
				Assert.StartsWith(expectedFontFamily, nativeReference.FontFamily.Source, StringComparison.Ordinal);
				Assert.Equal(WFontWeights.Bold.Weight, nativeReference.FontWeight.Weight);
				Assert.Equal(WVerticalAlignment.Bottom, nativeReference.VerticalAlignment);

				var navigationView = shell.CurrentItem.Handler.PlatformView as WNavigationView;
				Assert.NotNull(navigationView);
				await AssertEventually(
					() => navigationView.AutoSuggestBox?.IsLoaded == true
						&& FindTextBox(navigationView.AutoSuggestBox)?.IsLoaded == true,
					message: "The NavigationView SearchHandler text control did not load.");

				WAutoSuggestBox searchBox = navigationView.AutoSuggestBox;
				WTextBox nativeTextBox = FindTextBox(searchBox);
				Assert.NotNull(nativeTextBox);

				InvokeButton(fontSizeButton);
				InvokeButton(fontFamilyButton);
				InvokeButton(verticalTextAlignmentButton);
				InvokeButton(fontAttributesButton);

				await AssertEventually(
					() => clickedProperties.Count == 4 && changedProperties.Count == 4,
					message: "The four recorded button actions did not update all managed SearchHandler properties.");
				Assert.Equal(
					new[]
					{
						nameof(SearchHandler.FontSize),
						nameof(SearchHandler.FontFamily),
						nameof(SearchHandler.VerticalTextAlignment),
						nameof(SearchHandler.FontAttributes)
					},
					clickedProperties);
				Assert.Equal(clickedProperties, changedProperties);
				Assert.Equal(expectedFontSize, searchHandler.FontSize);
				Assert.Equal(expectedFontFamily, searchHandler.FontFamily);
				Assert.Equal(expectedVerticalTextAlignment, searchHandler.VerticalTextAlignment);
				Assert.Equal(expectedFontAttributes, searchHandler.FontAttributes);

				var dispatcherCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				Assert.True(nativeTextBox.DispatcherQueue.TryEnqueue(() =>
				{
					postActionState = clickedProperties.Count;
					dispatcherCompletion.SetResult();
				}));
				await dispatcherCompletion.Task;
				Assert.Equal(4, postActionState);

				await AssertEventually(
					() =>
					{
						bool fontSizeMatches = Math.Abs(nativeTextBox.FontSize - expectedFontSize) <= 0.01;
						bool fontFamilyMatches = nativeTextBox.FontFamily.Source.StartsWith(expectedFontFamily, StringComparison.Ordinal);
						bool fontWeightMatches = nativeTextBox.FontWeight.Weight == WFontWeights.Bold.Weight;
						bool verticalAlignmentMatches = nativeTextBox.VerticalContentAlignment == WVerticalAlignment.Bottom;
						return fontSizeMatches & fontFamilyMatches & fontWeightMatches & verticalAlignmentMatches;
					},
					message: $"{FailureSignature} native TextBox did not apply FontSize 32, Courier New, FontWeight 700, and bottom vertical alignment.");
			});
		}

		static bool IsLoaded(VisualElement element) =>
			element.Handler?.PlatformView is WFrameworkElement frameworkElement && frameworkElement.IsLoaded;

		static void InvokeButton(Button button)
		{
			var nativeButton = button.Handler.PlatformView as WButton;
			Assert.NotNull(nativeButton);
			var peer = new ButtonAutomationPeer(nativeButton);
			var invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
			Assert.NotNull(invokeProvider);
			invokeProvider.Invoke();
		}

		static WTextBox FindTextBox(WDependencyObject parent)
		{
			int childCount = WVisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childCount; i++)
			{
				var child = WVisualTreeHelper.GetChild(parent, i);
				if (child is WTextBox textBox)
					return textBox;

				var descendant = FindTextBox(child);
				if (descendant is not null)
					return descendant;
			}

			return null;
		}
	}
}
#endif

