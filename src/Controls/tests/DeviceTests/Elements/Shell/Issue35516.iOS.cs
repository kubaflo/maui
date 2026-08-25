using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Category(TestCategory.Shell)]
	[Category("Issue35516")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue35516 : ControlsHandlerTestBase
	{
		[Fact(DisplayName = "Changing SearchHandler Query Updates Native Search Text")]
		public async Task ChangingSearchHandlerQueryUpdatesNativeSearchText()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			const string expectedQuery = "Hello World";
			var searchHandler = new SearchHandler
			{
				AutomationId = "SearchHandlerControl"
			};
			var enterTextButton = new Button
			{
				AutomationId = "EnterTextButton",
				Text = "Enter Text"
			};
			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "SearchHandler Query update"
					},
					new Label
					{
						Text = "Tap Enter Text. The Shell search box should display Hello World."
					},
					enterTextButton
				}
			};
			var page = new ContentPage
			{
				Title = "Search Query",
				Content = content
			};
			Shell.SetSearchHandler(page, searchHandler);

			var shell = new Shell
			{
				Items =
				{
					new ShellContent
					{
						Title = "Search Query",
						ContentTemplate = new DataTemplate(() => page)
					}
				}
			};

			var triggerCount = -1;
			enterTextButton.Clicked += (sender, args) =>
			{
				triggerCount = triggerCount < 0 ? 1 : triggerCount + 1;
				searchHandler.Query = expectedQuery;
			};

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async handler =>
			{
				await OnLoadedAsync(page);
				await OnFrameSetToNotEmpty(content);

				UISearchBar nativeSearchBar = null;
				UISearchTextField nativeSearchTextField = null;
				UIButton nativeButton = null;
				await AssertEventually(() =>
				{
					var rootView = handler.ViewController.View;
					nativeSearchBar = FindDescendant<UISearchBar>(rootView, searchBar => true);
					nativeSearchTextField = nativeSearchBar?.SearchTextField;
					nativeButton = FindDescendant<UIButton>(
						rootView,
						button => button.CurrentTitle == enterTextButton.Text);
					return nativeSearchTextField is not null && nativeButton is not null;
				}, message: "The attached Shell did not render its native search text field and Enter Text button.");

				Assert.NotNull(nativeSearchTextField.Window);
				Assert.NotNull(nativeButton.Window);
				Assert.False(nativeSearchTextField.Hidden);
				Assert.True(nativeSearchTextField.Alpha > 0);
				Assert.True(string.IsNullOrEmpty(searchHandler.Query));
				Assert.True(string.IsNullOrEmpty(nativeSearchTextField.Text));
				using var initialRendering = await nativeSearchTextField.ToBitmap(MauiContext);

				nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				Assert.Equal(1, triggerCount);
				Assert.Equal(expectedQuery, searchHandler.Query);

				await AssertEventually(
					() => nativeSearchTextField.Text == expectedQuery,
					message: $"SearchHandler.Query dynamic update was not reflected in native UISearchTextField: expected '{expectedQuery}', observed '{nativeSearchTextField.Text ?? "<null>"}'.");

				using var updatedRendering = await nativeSearchTextField.ToBitmap(MauiContext);
				using var initialPixels = initialRendering.AsPNG();
				using var updatedPixels = updatedRendering.AsPNG();
				Assert.NotNull(initialPixels);
				Assert.NotNull(updatedPixels);
				Assert.False(
					initialPixels.IsEqual(updatedPixels),
					"SearchHandler.Query dynamic update was not reflected in native UISearchTextField: rendered pixels did not change after setting the query.");

				Assert.True(
					nativeSearchTextField.Text == expectedQuery,
					$"SearchHandler.Query dynamic update was not reflected in native UISearchTextField: expected '{expectedQuery}', observed '{nativeSearchTextField.Text ?? "<null>"}'.");
			});
		}

		static T FindDescendant<T>(UIView view, Func<T, bool> predicate)
			where T : UIView
		{
			if (view is T matchingView && predicate(matchingView))
				return matchingView;

			foreach (var subview in view.Subviews)
			{
				var match = FindDescendant(subview, predicate);
				if (match is not null)
					return match;
			}

			return null;
		}
	}
#endif
}

