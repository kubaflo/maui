#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue31044")]
	public class Issue31044 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ReplacingControlTemplateDisconnectsUnloadedHandlers()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			int clickCount = 0;
			int firstTemplateUnloadCount = 0;
			int replacementTemplateLoadCount = 0;
			var clickCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var firstTemplateUnloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var replacementTemplateLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			ContentView firstTemplateRoot = null;
			Label firstTemplateChild = null;
			ContentView replacementTemplateRoot = null;
			Label replacementTemplateChild = null;

			var firstTemplate = new ControlTemplate(() =>
			{
				firstTemplateChild = new Label
				{
					Text = "Template 1 is active",
					FontSize = 20,
					HorizontalOptions = LayoutOptions.Center
				};
				firstTemplateRoot = new ContentView
				{
					Padding = 24,
					Content = new VerticalStackLayout
					{
						Children =
						{
							firstTemplateChild,
							new Label { Text = "This visual tree will be unloaded." }
						}
					}
				};
				firstTemplateRoot.Unloaded += (_, _) =>
				{
					firstTemplateUnloadCount++;
					firstTemplateUnloaded.TrySetResult();
				};
				return firstTemplateRoot;
			});

			var secondTemplate = new ControlTemplate(() =>
			{
				replacementTemplateChild = new Label
				{
					Text = "Template 2 is active",
					FontSize = 20,
					HorizontalOptions = LayoutOptions.Center
				};
				replacementTemplateRoot = new ContentView
				{
					Padding = 24,
					Content = replacementTemplateChild
				};
				replacementTemplateRoot.Loaded += (_, _) =>
				{
					replacementTemplateLoadCount++;
					replacementTemplateLoaded.TrySetResult();
				};
				return replacementTemplateRoot;
			});

			var templateHost = new ContentView { ControlTemplate = firstTemplate };
			var statusLabel = new Label
			{
				Text = "Template 1 is active",
				FontAttributes = FontAttributes.Bold
			};
			var toggleButton = new Button { Text = "Toggle template" };
			toggleButton.Clicked += (_, _) =>
			{
				clickCount++;
				templateHost.ControlTemplate = secondTemplate;
				statusLabel.Text = "Template 2 is active";
				clickCompleted.TrySetResult();
			};

			var actions = new VerticalStackLayout
			{
				Spacing = 12,
				Children =
				{
					toggleButton,
					statusLabel
				}
			};
			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 16,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				}
			};
			var titleLabel = new Label
			{
				Text = "ContentView ControlTemplate lifecycle",
				FontSize = 24,
				FontAttributes = FontAttributes.Bold
			};
			var explanationLabel = new Label
			{
				Text = "The template below should release its handlers when replaced."
			};
			Grid.SetRow(explanationLabel, 1);
			Grid.SetRow(templateHost, 2);
			Grid.SetRow(actions, 3);
			grid.Children.Add(titleLabel);
			grid.Children.Add(explanationLabel);
			grid.Children.Add(templateHost);
			grid.Children.Add(actions);

			var page = new ContentPage { Content = grid };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(firstTemplateRoot);
				Assert.NotNull(firstTemplateChild);
				Assert.Same(firstTemplateChild, Assert.IsType<VerticalStackLayout>(firstTemplateRoot.Content).Children[0]);
				Assert.NotNull(firstTemplateRoot.Handler);
				Assert.NotNull(firstTemplateChild.Handler);
				Assert.NotNull(Assert.IsAssignableFrom<UIView>(firstTemplateRoot.Handler.PlatformView).Window);
				Assert.NotNull(Assert.IsAssignableFrom<UIView>(firstTemplateChild.Handler.PlatformView).Window);
				Assert.Equal(0, clickCount);
				Assert.Equal(0, firstTemplateUnloadCount);
				Assert.Equal(0, replacementTemplateLoadCount);
				Assert.False(clickCompleted.Task.IsCompleted);
				Assert.False(firstTemplateUnloaded.Task.IsCompleted);
				Assert.False(replacementTemplateLoaded.Task.IsCompleted);

				var nativeButton = Assert.IsAssignableFrom<UIButton>(toggleButton.Handler.PlatformView);
				Assert.NotNull(nativeButton.Window);
				nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await clickCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
				await firstTemplateUnloaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
				await replacementTemplateLoaded.Task.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.Equal(1, clickCount);
				Assert.Equal(1, firstTemplateUnloadCount);
				Assert.Equal(1, replacementTemplateLoadCount);
				Assert.Equal("Template 2 is active", statusLabel.Text);
				Assert.NotNull(replacementTemplateRoot);
				Assert.NotNull(replacementTemplateChild);
				Assert.NotSame(firstTemplateRoot, replacementTemplateRoot);
				Assert.Same(replacementTemplateChild, replacementTemplateRoot.Content);
				Assert.Equal("Template 2 is active", replacementTemplateChild.Text);
				Assert.NotNull(replacementTemplateRoot.Handler);
				Assert.NotNull(replacementTemplateChild.Handler);
				Assert.NotNull(Assert.IsAssignableFrom<UIView>(replacementTemplateRoot.Handler.PlatformView).Window);
				Assert.NotNull(Assert.IsAssignableFrom<UIView>(replacementTemplateChild.Handler.PlatformView).Window);

				bool handlersDisconnected = await Wait(
					() => firstTemplateRoot.Handler is null && firstTemplateChild.Handler is null,
					timeout: 1000);

				Assert.True(
					handlersDisconnected,
					$"Issue31044: unloaded template handlers should be null; root={firstTemplateRoot.Handler?.GetType().Name ?? "null"}, child={firstTemplateChild.Handler?.GetType().Name ?? "null"}");
			});
		}
	}
}
#endif

