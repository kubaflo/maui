using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests.Memory;

#if ANDROID
[Category("Issue36743")]
public class Issue36743 : ControlsHandlerTestBase
{
	const int BrushCount = 50;

	[Fact]
	public async Task ClearingSharedGradientStopsReleasesBrushes()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<IScrollView, ScrollViewHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<BoxView, BoxViewHandler>();
			});
		});

		var previewBox = new BoxView
		{
			HeightRequest = 60,
			CornerRadius = 8,
		};
		var layout = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 12,
			Children =
			{
				new Label
				{
					Text = "GradientStops.Clear shared-stop leak",
					FontSize = 18,
					FontAttributes = FontAttributes.Bold,
				},
				new Label
				{
					Text = "The preview uses brushes whose GradientStops share the same two GradientStop instances.",
					FontSize = 13,
					TextColor = Colors.Gray,
				},
				new Button { Text = "1. Create brushes using shared stops" },
				new Button { Text = "2. Clear GradientStops and release" },
				new Button { Text = "3. Check shared-stop subscriptions" },
				new Label
				{
					Text = "Ready to create brushes.",
					FontSize = 15,
					TextColor = Colors.DarkBlue,
				},
				previewBox,
			},
		};
		var page = new ContentPage
		{
			Content = new ScrollView { Content = layout },
		};
		ReproductionState state = null;

		await CreateHandlerAndAddToWindow(new Window(page), () =>
		{
			Assert.NotNull(previewBox.Handler);
			Assert.NotNull(previewBox.Handler.PlatformView);
			Assert.True(previewBox.Height > 0);

			state = CreateAndReleaseBrushes(previewBox);
		});

		Assert.NotNull(state);
		await AssertionExtensions.WaitForGC(state.BrushReferences);

		GC.KeepAlive(state.SharedStart);
		GC.KeepAlive(state.SharedEnd);
	}

	static ReproductionState CreateAndReleaseBrushes(BoxView previewBox)
	{
		var sharedStart = new GradientStop(Colors.DeepSkyBlue, 0f);
		var sharedEnd = new GradientStop(Colors.MediumVioletRed, 1f);
		var brushes = new List<LinearGradientBrush>(BrushCount);
		var boxes = new List<BoxView>(BrushCount);
		var brushReferences = new WeakReference[BrushCount];

		for (int i = 0; i < BrushCount; i++)
		{
			var brush = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 1),
			};
			brush.GradientStops.Add(sharedStart);
			brush.GradientStops.Add(sharedEnd);

			var box = new BoxView
			{
				HeightRequest = 4,
				Background = brush,
			};

			brushes.Add(brush);
			boxes.Add(box);
			brushReferences[i] = new WeakReference(brush);
		}

		previewBox.Background = brushes[0];
		Assert.Same(brushes[0], previewBox.Background);

		int clearedCount = -1;
		clearedCount = 0;
		foreach (var brush in brushes)
		{
			brush.GradientStops.CollectionChanged += (_, args) =>
			{
				Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action);
				clearedCount++;
			};
			brush.GradientStops.Clear();
			Assert.Empty(brush.GradientStops);
		}

		previewBox.Background = null;
		boxes.Clear();
		brushes.Clear();

		Assert.Equal(BrushCount, clearedCount);
		return new ReproductionState
		{
			BrushReferences = brushReferences,
			SharedStart = sharedStart,
			SharedEnd = sharedEnd,
		};
	}

	sealed class ReproductionState
	{
		public WeakReference[] BrushReferences { get; init; }
		public GradientStop SharedStart { get; init; }
		public GradientStop SharedEnd { get; init; }
	}
}
#endif

