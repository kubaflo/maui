#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Shell)]
	[Category("Issue35624")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue35624 : ControlsHandlerTestBase
	{
		const string ExpectedQuery = "MAUISEARCH";

		[Fact]
		public async Task SearchHandlerCharacterSpacingIsAppliedToEnteredText()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			string observedQuery = null;
			var searchHandler = new SearchHandler
			{
				CharacterSpacing = 10,
				Placeholder = "SearchHandler spacing",
				SearchBoxVisibility = SearchBoxVisibility.Expanded
			};
			searchHandler.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName == nameof(SearchHandler.Query))
					observedQuery = searchHandler.Query;
			};
			var pageCreated = new TaskCompletionSource<ContentPage>();
			var shellContent = new ShellContent
			{
				Title = "Sandbox",
				Route = "MainPage",
				ContentTemplate = new DataTemplate(() =>
				{
					var page = new ContentPage
					{
						Title = "Sandbox",
						Content = new VerticalStackLayout
						{
							Padding = 24,
							Spacing = 16,
							Children =
							{
								new Label { FontAttributes = FontAttributes.Bold, FontSize = 22, Text = "SearchHandler CharacterSpacing" },
								new Label { Text = "Configured CharacterSpacing: 10" },
								new Label { Text = "Tap the Shell search field above and type MAUISEARCH." },
								new Label { Text = "Entered text appears in the search field above." },
								new Label { FontAttributes = FontAttributes.Bold, Text = "Expected: increased character spacing." }
							}
						}
					};

					Shell.SetSearchHandler(page, searchHandler);
					pageCreated.TrySetResult(page);
					return page;
				})
			};
			var shell = new Shell
			{
				Items = { shellContent }
			};
			var window = new Microsoft.Maui.Controls.Window(shell);

			await CreateHandlerAndAddToWindow<ShellRenderer>(window, async handler =>
			{
				var page = await pageCreated.Task.WaitAsync(TimeSpan.FromSeconds(2));
				await OnLoadedAsync(page);

				UISearchBar nativeSearchBar = null;
				UITextField nativeTextField = null;
				await AssertEventually(() =>
				{
					var toolbar = GetPlatformToolbar(handler);
					var searchBar = toolbar?.TopItem?.SearchController?.SearchBar;
					var textField = searchBar?.FindDescendantView<UITextField>();
					if (searchBar is null || textField is null)
						return false;

					nativeSearchBar = searchBar;
					nativeTextField = textField;
					return true;
				}, message: "The expanded SearchHandler native field was not created.");

				Assert.Equal("SearchHandler spacing", nativeTextField.Placeholder);
				Assert.Equal(string.Empty, nativeTextField.Text ?? string.Empty);
				Assert.Same(nativeSearchBar, GetPlatformToolbar(handler).TopItem.SearchController.SearchBar);

				nativeTextField.BecomeFirstResponder();
				await AssertEventually(
					() => nativeTextField.IsFirstResponder,
					message: "The SearchHandler native field did not become first responder.");

				nativeTextField.InsertText(ExpectedQuery);
				await AssertEventually(
					() => observedQuery == ExpectedQuery && searchHandler.Query == ExpectedQuery,
					message: "The SearchHandler query callback did not receive MAUISEARCH.");
				await AssertEventually(
					() => nativeTextField.AttributedText?.Value == ExpectedQuery,
					message: "The SearchHandler native field did not render the entered query.");

				var attributedText = nativeTextField.AttributedText;
				Assert.NotNull(attributedText);
				Assert.Equal(ExpectedQuery, attributedText.Value);
				Assert.Equal(ExpectedQuery.Length, (int)attributedText.Length);

				var kerningAttribute = attributedText.GetAttribute(
					UIStringAttributeKey.KerningAdjustment,
					0,
					out var attributeRange);
				Assert.Equal(0, (int)attributeRange.Location);
				Assert.Equal(ExpectedQuery.Length, (int)attributeRange.Length);

				var kerning = kerningAttribute as NSNumber;
				var measuredCharacterSpacing = kerning is null ? double.NaN : kerning.DoubleValue;
				Assert.True(
					!double.IsNaN(measuredCharacterSpacing) &&
					Math.Abs(measuredCharacterSpacing - searchHandler.CharacterSpacing) <= 0.01,
					$"SearchHandler native character spacing was {(double.IsNaN(measuredCharacterSpacing) ? "missing" : measuredCharacterSpacing)}; expected {searchHandler.CharacterSpacing} for text '{attributedText.Value}' in range {attributeRange}.");
			});
		}

	}
}
#endif

