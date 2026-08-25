using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Hosting;
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
		public async Task SearchHandlerRuntimeFontPropertiesUpdateNativeSearchBox()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.SetupShellHandlers();
				});
			});

			var searchHandler = new SearchHandler
			{
				Placeholder = "Search here",
				SearchBoxVisibility = SearchBoxVisibility.Expanded
			};
			var changedProperties = new HashSet<string>();
			searchHandler.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName is not null)
					changedProperties.Add(args.PropertyName);
			};

			int actionCount = 0;
			var actionStatusLabel = new Label
			{
				AutomationId = "ActionStatusLabel",
				Text = "Actions applied: 0/4"
			};
			var fontSizeButton = CreateButton("FontSizeButton", "Set FontSize to 28", () => searchHandler.FontSize = 28);
			var fontFamilyButton = CreateButton("FontFamilyButton", "Set FontFamily to Courier New", () => searchHandler.FontFamily = "Courier New");
			var verticalAlignmentButton = CreateButton("VerticalAlignmentButton", "Set VerticalTextAlignment to End", () => searchHandler.VerticalTextAlignment = TextAlignment.End);
			var fontAttributesButton = CreateButton("FontAttributesButton", "Set FontAttributes to Bold", () => searchHandler.FontAttributes = FontAttributes.Bold);

			var page = new ContentPage
			{
				Title = "SearchHandler styling",
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 12,
						Children =
						{
							new Label
							{
								Text = "The Shell search box above should become 28pt, Courier New, bottom-aligned, and bold.",
								FontSize = 18
							},
							actionStatusLabel,
							fontSizeButton,
							fontFamilyButton,
							verticalAlignmentButton,
							fontAttributesButton
						}
					}
				}
			};
			Shell.SetSearchHandler(page, searchHandler);

			var shell = new Shell();
			shell.Items.Add(new ShellContent
			{
				Title = "SearchHandler styling",
				Route = "MainPage",
				Content = page
			});

			await CreateHandlerAndAddToWindow<ShellHandler>(shell, async _ =>
			{
				await AssertEventually(() => shell.CurrentItem?.Handler?.PlatformView is MauiNavigationView);

				var navigationView = Assert.IsType<MauiNavigationView>(shell.CurrentItem.Handler.PlatformView);
				var nativeSearchBox = Assert.IsType<WAutoSuggestBox>(navigationView.AutoSuggestBox);
				Assert.Equal("Search here", nativeSearchBox.PlaceholderText);
				Assert.True(nativeSearchBox.ActualWidth > 0 && nativeSearchBox.ActualHeight > 0);
				Assert.Same(nativeSearchBox, navigationView.AutoSuggestBox);

				double initialFontSize = nativeSearchBox.FontSize;
				string initialFontFamily = nativeSearchBox.FontFamily.Source;
				var initialVerticalAlignment = nativeSearchBox.VerticalContentAlignment;
				ushort initialFontWeight = nativeSearchBox.FontWeight.Weight;
				Assert.False(
					Math.Abs(initialFontSize - 28) <= 0.01 &&
					initialFontFamily == "Courier New" &&
					initialVerticalAlignment == WVerticalAlignment.Bottom &&
					initialFontWeight >= 700);

				Invoke(fontSizeButton);
				Invoke(fontFamilyButton);
				Invoke(verticalAlignmentButton);
				Invoke(fontAttributesButton);

				await AssertEventually(
					() => actionCount == 4 &&
						changedProperties.Contains(nameof(SearchHandler.FontSize)) &&
						changedProperties.Contains(nameof(SearchHandler.FontFamily)) &&
						changedProperties.Contains(nameof(SearchHandler.VerticalTextAlignment)) &&
						changedProperties.Contains(nameof(SearchHandler.FontAttributes)),
					message: "All four SearchHandler runtime property transitions should complete.");

				Assert.Equal(28, searchHandler.FontSize);
				Assert.Equal("Courier New", searchHandler.FontFamily);
				Assert.Equal(TextAlignment.End, searchHandler.VerticalTextAlignment);
				Assert.Equal(FontAttributes.Bold, searchHandler.FontAttributes);

				bool stylesApplied =
					Math.Abs(nativeSearchBox.FontSize - searchHandler.FontSize) <= 0.01 &&
					nativeSearchBox.FontFamily.Source == searchHandler.FontFamily &&
					nativeSearchBox.VerticalContentAlignment == WVerticalAlignment.Bottom &&
					nativeSearchBox.FontWeight.Weight >= 700;
				Assert.True(stylesApplied,
					$"SearchHandler native styles after runtime updates: FontSize={nativeSearchBox.FontSize} expected 28; " +
					$"FontFamily={nativeSearchBox.FontFamily.Source} expected Courier New; " +
					$"VerticalContentAlignment={nativeSearchBox.VerticalContentAlignment} expected Bottom; " +
					$"FontWeight={nativeSearchBox.FontWeight.Weight} expected at least 700.");
			});

			Button CreateButton(string automationId, string text, Action update)
			{
				var button = new Button
				{
					AutomationId = automationId,
					Text = text
				};
				button.Clicked += (_, _) =>
				{
					update();
					actionCount++;
					actionStatusLabel.Text = $"Actions applied: {actionCount}/4";
				};
				return button;
			}

			static void Invoke(Button button)
			{
				var nativeButton = Assert.IsAssignableFrom<WButton>(button.ToPlatform());
				var peer = new ButtonAutomationPeer(nativeButton);
				var invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke));
				invokeProvider.Invoke();
			}
		}
	}
}

