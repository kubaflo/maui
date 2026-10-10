using System.Threading.Tasks;
using Android.Widget;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;
using MauiButton = Microsoft.Maui.Controls.Button;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Shell)]
	[Category("Issue32043")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue32043 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DefaultShellTitleIsAccessibilityHeading()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.SetupShellHandlers();
					handlers.AddHandler<Window, WindowHandlerStub>();
				});
			});

			var calibrationLabel = new Label { Text = "Heading calibration" };
			SemanticProperties.SetHeadingLevel(calibrationLabel, SemanticHeadingLevel.Level1);

			await CreateHandlerAndAddToWindow<IWindowHandler>(
				new Window(new ContentPage { Content = calibrationLabel }),
				_ =>
				{
					var calibrationTextView = Assert.IsAssignableFrom<TextView>(calibrationLabel.Handler.PlatformView);
					Assert.True(calibrationTextView.AccessibilityHeading);
				});

			var contentPage = new ContentPage
			{
				Title = "Home",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 20,
					Children =
					{
						new Label { Text = "The Shell title should be exposed as a TalkBack heading." },
						new Label { Text = "Heading state" },
						new MauiButton { Text = "Check heading semantics" }
					}
				}
			};

			var shellContent = new ShellContent
			{
				Title = "Home",
				ContentTemplate = new DataTemplate(() => contentPage)
			};
			var shell = new Shell();
			shell.Items.Add(shellContent);
			var window = new Window(shell);

			var attachmentCallbackRan = false;
			TextView titleView = null;

			await CreateHandlerAndAddToWindow<IWindowHandler>(window, async _ =>
			{
				attachmentCallbackRan = true;
				var shellRenderer = Assert.IsType<ShellRenderer>(shell.Handler);

				await AssertEventually(() =>
				{
					var toolbar = GetPlatformToolbar(shellRenderer);
					titleView = toolbar is null ? null : FindTextView(toolbar, "Home");
					return titleView is not null;
				}, message: "The default Shell title TextView was not materialized.");

				Assert.True(attachmentCallbackRan);
				Assert.Null(Shell.GetTitleView(contentPage));
				Assert.NotNull(((IPlatformViewHandler)shellRenderer).PlatformView);
				Assert.NotNull(titleView);
				Assert.Equal("Home", titleView.Text);
				Assert.True(
					titleView.AccessibilityHeading,
					$"Default Shell title was not exposed as an Android accessibility heading. Title='{titleView.Text}', AccessibilityHeading={titleView.AccessibilityHeading}, expected=True.");
			});
		}

		static TextView FindTextView(AView view, string text)
		{
			if (view is TextView textView && textView.Text == text)
				return textView;

			if (view is AViewGroup viewGroup)
			{
				for (var index = 0; index < viewGroup.ChildCount; index++)
				{
					var child = viewGroup.GetChildAt(index);
					if (child is not null && FindTextView(child, text) is { } match)
						return match;
				}
			}

			return null;
		}
	}
}

