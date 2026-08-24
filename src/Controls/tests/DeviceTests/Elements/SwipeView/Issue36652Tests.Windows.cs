#if WINDOWS
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36652")]
	public class Issue36652 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task BorderIsolatesSwipeViewFromCompositionClipping()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<SwipeView, SwipeViewHandler>();
					handlers.AddHandler<SwipeItem, SwipeItemMenuItemHandler>();
					handlers.AddHandler<Editor, EditorHandler>();
				});
			});

			var leftItems = new SwipeItems
			{
				new SwipeItem { Text = "Back" }
			};
			leftItems.Mode = SwipeMode.Execute;

			var editor = new Editor();
			var swipeView = new SwipeView
			{
				Threshold = 80,
				LeftItems = leftItems,
				Content = editor
			};
			var border = new Border
			{
				Stroke = Colors.DarkGray,
				StrokeThickness = 1,
				Content = swipeView
			};
			var page = new ContentPage
			{
				Content = border
			};

			await InvokeOnMainThreadAsync(() =>
			{
				CreateHandler<PageHandler>(page);
				var borderHandler = Assert.IsType<BorderHandler>(border.Handler);
				var swipeViewHandler = Assert.IsType<SwipeViewHandler>(swipeView.Handler);
				var editorHandler = Assert.IsType<EditorHandler>(editor.Handler);
				var nativeBorder = borderHandler.PlatformView;
				Assert.Same(editorHandler.PlatformView, swipeViewHandler.PlatformView.Content);

				Assert.True(
					swipeViewHandler.NeedsContainer &&
					nativeBorder.Content is WrapperView protectedContent &&
					object.ReferenceEquals(swipeViewHandler.PlatformView, protectedContent.Child),
					"Border must isolate SwipeView from composition clipping.");
			});
		}
	}
}
#endif

