#if IOS && !MACCATALYST
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue30084")]
	public class Issue30084 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task UppercaseSearchBarRaisesTextChangedOncePerCharacter()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<SearchBar, SearchBarHandler>();
				});
			});

			var searchBar = new SearchBar
			{
				Placeholder = "Enter lowercase d",
				TextTransform = TextTransform.Uppercase,
			};
			InputView inputView = searchBar;

			var page = new ContentPage
			{
				Title = "Issue 30084",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							FontAttributes = FontAttributes.Bold,
							FontSize = 24,
							Text = "TextTransform behavior",
						},
						new Label { Text = "Type one lowercase d into the default-styled uppercase SearchBar." },
						searchBar,
					},
				},
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var searchBarHandler = Assert.IsType<SearchBarHandler>(searchBar.Handler);
				var platformSearchBar = Assert.IsType<MauiSearchBar>(searchBarHandler.PlatformView);
				var platformEditor = Assert.IsAssignableFrom<UITextField>(searchBarHandler.QueryEditor);

				Assert.NotNull(platformSearchBar.Window);
				Assert.NotNull(platformEditor.Window);
				Assert.True(string.IsNullOrEmpty(inputView.Text));
				Assert.True(string.IsNullOrEmpty(platformSearchBar.Text));
				Assert.True(string.IsNullOrEmpty(platformEditor.Text));
				Assert.Equal(TextTransform.Uppercase, inputView.TextTransform);
				Assert.True(platformEditor.BecomeFirstResponder());

				var textChangedCount = -1;
				var callbackObserved = false;
				var finalNewValue = "<not observed>";
				var transitions = new List<string>();

				inputView.TextChanged += (_, args) =>
				{
					textChangedCount = textChangedCount < 0 ? 1 : textChangedCount + 1;
					callbackObserved = true;
					finalNewValue = args.NewTextValue;
					transitions.Add($"{args.OldTextValue ?? "<empty>"} -> {args.NewTextValue ?? "<empty>"}");
				};

				platformEditor.InsertText("d");

				await AssertEventually(() => callbackObserved);
				await AssertEventually(() => inputView.Text == "D");
				await AssertEventually(() => platformSearchBar.Text == "D");
				await AssertEventually(() => platformEditor.Text == "D");

				Assert.Equal("D", finalNewValue);
				Assert.True(
					textChangedCount == 1,
					$"One lowercase character should raise TextChanged once; observed count={textChangedCount}, transitions={string.Join(", ", transitions)}");
			});
		}
	}
}
#endif

