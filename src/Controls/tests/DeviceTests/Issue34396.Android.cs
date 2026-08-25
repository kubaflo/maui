using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Layout)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue34396 : ControlsHandlerTestBase
	{
		const int EntryCount = 201;
		const double MaximumResponsiveElapsedMilliseconds = 100;

		[Fact]
		[Category("Issue34396")]
		public async Task AddingEntriesDoesNotBlockQueuedUiWork()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
				});
			});

			var addEditorsButton = new Button { Text = "Add 200 Editors" };
			var responsivenessButton = new Button { Text = "Clicked 0" };
			var toolbar = new HorizontalStackLayout
			{
				Spacing = 8,
				Children =
				{
					addEditorsButton,
					responsivenessButton,
				}
			};
			var statusLabel = new Label { Text = "Ready" };
			var canvasColor = Color.FromArgb("#202020");
			var canvas = new AbsoluteLayout
			{
				WidthRequest = 2000,
				HeightRequest = 3000,
				BackgroundColor = canvasColor,
			};
			var scrollView = new ScrollView { Content = canvas };
			var root = new Grid
			{
				Padding = new Thickness(12),
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
				}
			};
			root.Add(toolbar);
			Grid.SetRow(statusLabel, 1);
			root.Add(statusLabel);
			Grid.SetRow(scrollView, 2);
			root.Add(scrollView);
			var page = new ContentPage { Content = root };

			var entries = new List<Entry>(EntryCount);
			var clicked = false;
			var callbackQueued = false;
			double elapsedMilliseconds = -1;
			var callbackCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			addEditorsButton.Clicked += (_, _) =>
			{
				clicked = true;
				var stopwatch = Stopwatch.StartNew();

				for (int i = 0; i < EntryCount; i++)
				{
					var entry = new Entry();
					entries.Add(entry);
					canvas.Children.Add(entry);
					AbsoluteLayout.SetLayoutBounds(
						entry,
						new Rect((i % 10) * 180, (i / 10) * 120, 160, 90));
				}

				callbackQueued = addEditorsButton.Dispatcher.Dispatch(() =>
				{
					elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
					callbackCompleted.SetResult();
				});
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				AssertHandlerAttached(page);
				AssertHandlerAttached(root);
				AssertHandlerAttached(toolbar);
				AssertHandlerAttached(addEditorsButton);
				AssertHandlerAttached(responsivenessButton);
				AssertHandlerAttached(statusLabel);
				AssertHandlerAttached(scrollView);
				AssertHandlerAttached(canvas);
				Assert.Empty(canvas.Children);
				Assert.Equal("Add 200 Editors", addEditorsButton.Text);
				Assert.Equal(new Thickness(12), root.Padding);
				Assert.Equal(3, root.RowDefinitions.Count);
				Assert.Equal(8, toolbar.Spacing);
				Assert.Equal(2000, canvas.WidthRequest);
				Assert.Equal(3000, canvas.HeightRequest);
				Assert.Equal(canvasColor, canvas.BackgroundColor);

				var buttonHandler = Assert.IsType<ButtonHandler>(addEditorsButton.Handler);
				buttonHandler.PlatformView.PerformClick();

				await callbackCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

				Assert.True(clicked);
				Assert.True(callbackQueued);
				Assert.NotEqual(-1, elapsedMilliseconds);
				Assert.Equal(EntryCount, entries.Count);
				Assert.Equal(EntryCount, canvas.Children.Count);

				for (int i = 0; i < EntryCount; i++)
				{
					var entry = entries[i];
					Assert.Same(entry, canvas.Children[i]);
					Assert.Equal(
						new Rect((i % 10) * 180, (i / 10) * 120, 160, 90),
						AbsoluteLayout.GetLayoutBounds(entry));
					var entryHandler = Assert.IsType<EntryHandler>(entry.Handler);
					Assert.NotNull(entryHandler.PlatformView);
				}

				Assert.True(
					elapsedMilliseconds < MaximumResponsiveElapsedMilliseconds,
					$"Issue34396 UI thread remained blocked after adding 201 Entry children; measured {elapsedMilliseconds:F0} ms, expected less than {MaximumResponsiveElapsedMilliseconds:F0} ms.");
			});
		}

		static void AssertHandlerAttached(IElement element)
		{
			Assert.NotNull(element.Handler);
			Assert.NotNull(element.Handler.PlatformView);
		}
	}
}

