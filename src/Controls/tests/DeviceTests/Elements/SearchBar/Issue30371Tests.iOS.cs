#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30371")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue30371 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task PlaceholderUsesHorizontalTextAlignment()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<SearchBar, SearchBarHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var headingLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 20,
				Text = "SearchBar placeholder alignment"
			};
			var referenceLabel = new Label
			{
				HorizontalTextAlignment = TextAlignment.End,
				Text = "Right edge reference"
			};
			var searchBar = new SearchBar
			{
				HorizontalTextAlignment = TextAlignment.End,
				Placeholder = "Search placeholder"
			};
			var evaluationButton = new Button
			{
				Text = "Evaluate visible alignment"
			};
			var explanatoryLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = "Expected placeholder alignment"
			};
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16
			};
			layout.Add(headingLabel);
			layout.Add(referenceLabel);
			layout.Add(searchBar);
			layout.Add(evaluationButton);
			layout.Add(explanatoryLabel);

			var page = new ContentPage
			{
				Content = layout
			};

			var attached = false;
			var expectedAlignment = (UITextAlignment)(-1);
			var observedPlaceholderAlignment = (UITextAlignment)(-1);

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, handler =>
			{
				Assert.NotNull(handler.PlatformView);

				var searchBarHandler = Assert.IsType<SearchBarHandler>(searchBar.Handler);
				var platformSearchBar = searchBarHandler.PlatformView;
				Assert.NotNull(platformSearchBar);

				var editor = searchBarHandler.QueryEditor;
				Assert.NotNull(editor);
				Assert.NotNull(editor.Window);
				attached = true;

				Assert.True(string.IsNullOrEmpty(searchBar.Text));
				var attributedPlaceholder = editor.AttributedPlaceholder;
				Assert.NotNull(attributedPlaceholder);
				Assert.Equal(searchBar.Placeholder, attributedPlaceholder.Value);

				expectedAlignment = editor.EffectiveUserInterfaceLayoutDirection == UIUserInterfaceLayoutDirection.RightToLeft
					? UITextAlignment.Left
					: UITextAlignment.Right;
				Assert.Equal(expectedAlignment, editor.TextAlignment);

				var paragraphStyle = attributedPlaceholder.GetAttribute(
					UIStringAttributeKey.ParagraphStyle,
					0,
					out _)
					as NSParagraphStyle;
				if (paragraphStyle is not null)
					observedPlaceholderAlignment = paragraphStyle.Alignment;

				return Task.CompletedTask;
			});

			Assert.True(attached);
			Assert.True(
				observedPlaceholderAlignment == expectedAlignment,
				$"Issue30371 placeholder alignment mismatch: expected {expectedAlignment}, observed {observedPlaceholderAlignment}");
		}
	}
}
#endif

