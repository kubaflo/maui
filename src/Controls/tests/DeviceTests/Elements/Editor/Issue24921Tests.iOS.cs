#if IOS && !MACCATALYST
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Editor)]
	[Category("Issue24921")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue24921 : ControlsHandlerTestBase
	{
		const string EntryPlaceholder = "Example entry";
		const string EditorPlaceholder = "Example editor";

		[Fact]
		public async Task EditorPlaceholderIsNotAnIndependentAccessibilityElement()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Editor, EditorHandler>();
				});
			});

			var entry = new Entry
			{
				Placeholder = EntryPlaceholder,
				PlaceholderColor = Colors.Black,
				TextColor = Colors.Black
			};

			var editor = new Editor
			{
				Placeholder = EditorPlaceholder,
				PlaceholderColor = Colors.Black,
				TextColor = Colors.Black
			};

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						entry,
						editor
					}
				}
			};

			var attachedCallbackRan = false;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				attachedCallbackRan = true;

				await AssertEventually(
					() => entry.Handler is EntryHandler && editor.Handler is EditorHandler,
					message: "Entry and Editor handlers were not attached.");

				var entryHandler = Assert.IsType<EntryHandler>(entry.Handler);
				var editorHandler = Assert.IsType<EditorHandler>(editor.Handler);
				var nativeEntry = entryHandler.PlatformView;
				var nativeEditor = editorHandler.PlatformView;

				await AssertEventually(
					() => nativeEntry.Window is not null && nativeEditor.Window is not null,
					message: "Entry and Editor native views were not attached to a window.");
				await AssertEventually(
					() => nativeEditor.Subviews.OfType<MauiLabel>().Any(label => label.Text == EditorPlaceholder),
					message: "The Editor placeholder label was not created.");

				Assert.NotNull(nativeEntry.Window);
				Assert.NotNull(nativeEditor.Window);

				var placeholder = Assert.Single(
					nativeEditor.Subviews.OfType<MauiLabel>().Where(label => label.Text == EditorPlaceholder));

				Assert.Same(nativeEditor, placeholder.Superview);
				Assert.True(placeholder.AccessibilityElementsHidden,
					"Issue24921: Editor placeholder must be hidden from the iOS accessibility hierarchy.");
			});

			Assert.True(attachedCallbackRan, "The attached-window callback did not run.");
		}
	}
}
#endif

