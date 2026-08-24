#if !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Shell)]
	[Category("Issue35667")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue35667 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task SearchHandlerUppercaseTransformAppliesToTypedText()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
				});
			});

			var instructionLabel = new Label
			{
				Text = "Type MixedCase in the Shell search bar. TextTransform.Uppercase should display MIXEDCASE.",
				AutomationId = "InstructionsLabel"
			};
			var statusLabel = new Label
			{
				Text = "Search text has not been entered.",
				AutomationId = "ResultStatus",
				FontAttributes = FontAttributes.Bold
			};
			var contentPage = new ContentPage
			{
				Title = "Search Transform",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children = { instructionLabel, statusLabel }
				}
			};
			var searchHandler = new SearchHandler
			{
				AutomationId = "SearchInput",
				Placeholder = "Type mixed case text",
				TextTransform = TextTransform.Uppercase
			};
			Shell.SetSearchHandler(contentPage, searchHandler);

			var shell = new Shell
			{
				Items =
				{
					new ShellContent
					{
						Title = "Search Transform",
						Route = "Issue35667",
						Content = contentPage
					}
				}
			};

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async handler =>
			{
				await OnLoadedAsync(contentPage);
				await OnNavigatedToAsync(contentPage);

				UISearchBar nativeSearchBar = null;
				await AssertEventually(
					() => (nativeSearchBar = handler.View.FindDescendantView<UISearchBar>()) is not null,
					timeout: 2000,
					message: "ShellRenderer did not create a native UISearchBar.");

				var nativeTextField = nativeSearchBar.FindDescendantView<UITextField>();
				Assert.True(handler.View.Window is not null, "ShellRenderer was not attached to a native window.");
				Assert.True(nativeSearchBar.Window is not null, "The native UISearchBar was not attached to a window.");
				Assert.True(nativeTextField is not null, "The native UISearchBar did not contain a UITextField.");
				Assert.True(string.IsNullOrEmpty(nativeTextField.Text), $"The native search field should start empty, but was '{nativeTextField.Text}'.");
				Assert.Equal("Type mixed case text", nativeSearchBar.Placeholder);
				Assert.Equal(TextTransform.Uppercase, searchHandler.TextTransform);

				const string input = "MixedCase";
				var expected = input.ToUpperInvariant();
				var observedQuery = "<query callback not observed>";
				var queryChanged = false;
				searchHandler.PropertyChanged += (_, e) =>
				{
					if (e.PropertyName == SearchHandler.QueryProperty.PropertyName &&
						!string.IsNullOrEmpty(searchHandler.Query))
					{
						observedQuery = searchHandler.Query;
						queryChanged = true;
					}
				};

				Assert.True(nativeTextField.BecomeFirstResponder(), "The native search field could not become first responder.");
				nativeTextField.InsertText(input);

				await AssertEventually(
					() => queryChanged,
					timeout: 2000,
					message: "SearchHandler.Query did not receive the native text input.");
				Assert.NotEqual("<query callback not observed>", observedQuery);
				await AssertEventually(
					() => !string.IsNullOrEmpty(nativeSearchBar.Text),
					timeout: 2000,
					message: "The native UISearchBar did not display the typed text.");

				var actual = nativeSearchBar.Text;
				Assert.True(
					string.Equals(expected, actual, StringComparison.Ordinal),
					$"Shell SearchHandler native text after typing was '{actual}', expected '{expected}'.");
			});
		}
	}
}
#endif

