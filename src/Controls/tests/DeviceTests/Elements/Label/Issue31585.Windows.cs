#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WFlowDirection = Microsoft.UI.Xaml.FlowDirection;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue31585")]
	public class Issue31585 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RightToLeftLabelMirrorsNativePadding()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
				});
			});

			var arrangedPadding = new Thickness(50, 0, 0, 0);
			var rtlScene = CreateScene(FlowDirection.RightToLeft, arrangedPadding);

			await AttachAndRun<LayoutHandler>(rtlScene.Root, async _ =>
			{
				await rtlScene.LoadedTask;
				Assert.True(rtlScene.WasLoaded(), "RTL Label Loaded callback did not run");

				var labelHandler = Assert.IsType<LabelHandler>(rtlScene.Label.Handler);
				var platformLabel = Assert.IsType<Microsoft.UI.Xaml.Controls.TextBlock>(labelHandler.PlatformView);

				Assert.Same(rtlScene.Label, labelHandler.VirtualView);
				Assert.Equal("My Label", rtlScene.Label.Text);
				Assert.True(platformLabel.IsLoaded);
				Assert.True(platformLabel.ActualWidth > 0);
				Assert.True(platformLabel.ActualHeight > 0);
				Assert.Equal(WFlowDirection.RightToLeft, platformLabel.FlowDirection);
				Assert.Equal(arrangedPadding, rtlScene.Label.Padding);
				Assert.Equal(0, platformLabel.Padding.Left);
				Assert.Equal(50, platformLabel.Padding.Right);
			});
		}

		static (Grid Root, Label Label, Task LoadedTask, Func<bool> WasLoaded) CreateScene(
			FlowDirection flowDirection,
			Thickness padding)
		{
			var loaded = false;
			var loadedSource = new TaskCompletionSource();
			var label = new Label
			{
				Text = "My Label",
				Padding = padding,
				FlowDirection = flowDirection,
				BackgroundColor = Colors.Yellow,
			};
			label.Loaded += (_, _) =>
			{
				loaded = true;
				loadedSource.TrySetResult();
			};

			var stack = new VerticalStackLayout
			{
				WidthRequest = 350,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Spacing = 0,
				Children =
				{
					label,
					new BoxView
					{
						Color = Colors.Red,
						HeightRequest = 100,
					},
				},
			};

			return (new Grid { Children = { stack } }, label, loadedSource.Task, () => loaded);
		}
	}
}
#endif

