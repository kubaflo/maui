using System;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if IOS && !MACCATALYST
	[Category(TestCategory.Shell)]
	[Category("Issue35624")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue35624 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CharacterSpacingIsAppliedToEnteredSearchText()
		{
			const string enteredText = "SPACED";
			const string querySentinel = "NOT_OBSERVED";
			const double expectedTolerance = 0.01;

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
				CharacterSpacing = 8,
				Placeholder = "Search",
				SearchBoxVisibility = SearchBoxVisibility.Expanded,
				ShowsResults = false
			};

			var queryCallbackOccurred = false;
			var observedQuery = querySentinel;
			searchHandler.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName == SearchHandler.QueryProperty.PropertyName)
				{
					queryCallbackOccurred = true;
					observedQuery = searchHandler.Query;
				}
			};

			var shellContent = new ShellContent
			{
				Title = "SearchHandler spacing",
				ContentTemplate = new DataTemplate(() =>
				{
					var page = new ContentPage
					{
						Title = "SearchHandler spacing",
						Content = new VerticalStackLayout
						{
							Padding = 24,
							Spacing = 18,
							Children =
							{
								new Label
								{
									FontSize = 20,
									Text = "Type SPACED in the Shell search field. The reference below uses CharacterSpacing 8."
								},
								new Label
								{
									CharacterSpacing = 8,
									FontSize = 24,
									Text = enteredText
								},
								new Button
								{
									Text = "Check search spacing"
								},
								new Label
								{
									FontSize = 18,
									Text = "Waiting for search input."
								}
							}
						}
					};

					Shell.SetSearchHandler(page, searchHandler);
					return page;
				})
			};

			var shell = new Shell
			{
				FlyoutBehavior = FlyoutBehavior.Disabled,
				Items = { shellContent }
			};

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async handler =>
			{
				var searchBar = FindSearchBar(handler.View);
				Assert.NotNull(searchBar);

				var searchField = searchBar.SearchTextField;
				Assert.NotNull(searchField);
				Assert.Equal(8d, searchHandler.CharacterSpacing);
				Assert.Equal(string.Empty, searchHandler.Query);

				Assert.True(searchField.BecomeFirstResponder(), "The native search field did not accept focus.");
				await AssertEventually(
					() => searchField.IsFirstResponder,
					timeout: 5000,
					message: "The native search field did not become focused.");

				searchField.InsertText(enteredText);

				await AssertEventually(
					() => queryCallbackOccurred,
					timeout: 5000,
					message: "SearchHandler did not raise a Query property change.");
				await AssertEventually(
					() => observedQuery == enteredText,
					timeout: 5000,
					message: $"SearchHandler Query callback observed '{observedQuery}'.");
				await AssertEventually(
					() => searchField.Text == enteredText,
					timeout: 5000,
					message: $"Native search text was '{searchField.Text}'.");
				await AssertEventually(
					() => searchField.AttributedText is not null && searchField.AttributedText.Length == enteredText.Length,
					timeout: 5000,
					message: "Native attributed search text was not updated.");

				var attributedText = searchField.AttributedText;
				Assert.NotNull(attributedText);
				Assert.Equal(enteredText, attributedText.Value);
				Assert.True(attributedText.Length > 0, "Native attributed search text did not contain index 0.");

				var kerning = attributedText.GetAttribute(UIStringAttributeKey.KerningAdjustment, 0, out var kerningRange);
				var appliedSpacing = (kerning as NSNumber)?.DoubleValue ?? 0;
				var expectedSpacing = searchHandler.CharacterSpacing;
				var spacingCoversEnteredText =
					kerningRange.Location == 0 &&
					kerningRange.Length == attributedText.Length;

				Assert.True(
					spacingCoversEnteredText &&
					Math.Abs(appliedSpacing - expectedSpacing) < expectedTolerance,
					$"SearchHandler native character spacing was {appliedSpacing} over range {kerningRange}, expected {expectedSpacing} over the complete entered text.");
			});
		}

		static UISearchBar FindSearchBar(UIView view)
		{
			if (view is UISearchBar searchBar)
				return searchBar;

			if (view is null)
				return null;

			foreach (var child in view.Subviews)
			{
				var foundSearchBar = FindSearchBar(child);
				if (foundSearchBar is not null)
					return foundSearchBar;
			}

			return null;
		}
	}
#endif
}

