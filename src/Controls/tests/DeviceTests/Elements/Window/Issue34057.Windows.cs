#if WINDOWS
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using ControlsWindow = Microsoft.Maui.Controls.Window;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue34057")]
	public class Issue34057 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task PopupAnimationCanStartWhileChildWindowIsDestroying()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<ControlsWindow, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Border, BorderHandler>();
				});
			});

			var scene = await InvokeOnMainThreadAsync(() =>
			{
				var savePopup = new Border
				{
					BackgroundColor = Colors.White,
					Padding = 24,
					Stroke = Colors.Gray,
					HorizontalOptions = LayoutOptions.End,
					VerticalOptions = LayoutOptions.End,
					Content = new VerticalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label
							{
								FontAttributes = FontAttributes.Bold,
								Text = "Save image"
							},
							new Label
							{
								Text = "Save popup"
							}
						}
					}
				};

				var viewer = new Grid
				{
					BackgroundColor = Colors.Black,
					Padding = 32,
					Children =
					{
						new Label
						{
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center,
							Text = "Image viewer",
							TextColor = Colors.White
						},
						savePopup
					}
				};

				var childPage = new ContentPage
				{
					Title = "Image viewer",
					Content = viewer
				};
				var childWindow = new ControlsWindow(childPage)
				{
					Title = "Image viewer"
				};

				return (SavePopup: savePopup, ChildPage: childPage, ChildWindow: childWindow);
			});
			var savePopup = scene.SavePopup;
			var childPage = scene.ChildPage;
			var childWindow = scene.ChildWindow;
			IAnimatable animationTarget = savePopup;

			var destroyingSignal = new TaskCompletionSource();
			var dispatchSignal = new TaskCompletionSource();
			var loaded = false;
			var destroyingCount = 0;
			var dispatchCount = 0;
			var outcome = "not-dispatched";
			var exceptionType = "not-observed";
			IMauiContext childMauiContext = null;

			childPage.Loaded += (_, _) => loaded = true;
			childWindow.Destroying += (_, _) =>
			{
				Interlocked.Increment(ref destroyingCount);
				destroyingSignal.TrySetResult();

				childWindow.Dispatcher.Dispatch(() =>
				{
					Interlocked.Increment(ref dispatchCount);
					try
					{
						AnimationExtensions.Animate(
							animationTarget,
							"HidePopup",
							value => savePopup.Opacity = value,
							1,
							0,
							length: 250);
						outcome = "started";
						exceptionType = "none";
					}
					catch (ObjectDisposedException exception)
					{
						outcome = "threw";
						exceptionType = exception.GetType().FullName;
					}
					finally
					{
						dispatchSignal.TrySetResult();
					}
				});
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(childWindow, async handler =>
			{
				var nativeWindow = Assert.IsType<MauiWinUIWindow>(handler.PlatformView);
				Assert.True(loaded, "The child page should be loaded before its window is closed.");
				Assert.Equal(1, savePopup.Opacity);
				Assert.NotNull(savePopup.Handler);
				Assert.NotNull(savePopup.Handler.PlatformView);

				childMauiContext = savePopup.Handler.MauiContext;
				Assert.NotNull(childMauiContext);

				nativeWindow.Close();

				await destroyingSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
				await dispatchSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
			});

			Assert.NotNull(childMauiContext);
			Assert.Equal(1, destroyingCount);
			Assert.Equal(1, dispatchCount);
			Assert.True(
				outcome == "started" && exceptionType == "none",
				$"AnimationExtensions.Animate should resolve IAnimationManager for the IAnimatable during Destroying without ObjectDisposedException. " +
				$"Outcome: {outcome}; exception: {exceptionType}; Destroying count: {destroyingCount}; dispatch count: {dispatchCount}.");
		}
	}
}
#endif

