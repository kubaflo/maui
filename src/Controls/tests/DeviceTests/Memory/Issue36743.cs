#if ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.DeviceTests.Memory;

[Category("Issue36743")]
public class Issue36743
{
	const int BrushCount = 50;

	[Fact]
	public async Task ClearingSharedGradientStopsDoesNotRetainBrushes()
	{
		var sharedStop1 = new GradientStop(Colors.DeepSkyBlue, 0f);
		var sharedStop2 = new GradientStop(Colors.MediumVioletRed, 1f);
		var previewBox = new BoxView
		{
			HeightRequest = 60,
			CornerRadius = 8
		};
		var layout = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 12,
			Children =
			{
				new Label
				{
					Text = "GradientBrush.GradientStops.Clear() Leak Repro",
					FontSize = 18,
					FontAttributes = FontAttributes.Bold
				},
				new Label
				{
					Text = "Shared GradientStops should not keep brushes alive after their GradientStops collections are cleared.",
					FontSize = 13,
					TextColor = Colors.Gray
				},
				new Button { Text = "1. Create brushes using shared stops" },
				new Button { Text = "2. Clear() GradientStops and release" },
				new Button { Text = "3. Force GC and check leaks" },
				new Label
				{
					Text = "Step 1: create the brushes.",
					FontSize = 14,
					TextColor = Colors.DarkBlue
				},
				previewBox
			}
		};
		var page = new ContentPage { Content = layout };

		var creation = CreateBrushes(sharedStop1, sharedStop2, previewBox);

		Assert.Same(layout, page.Content);
		AssertCreationState(creation, sharedStop1, sharedStop2, previewBox);

		bool clearAndReleaseCompleted = ClearAndRelease(creation, previewBox);
		Assert.True(clearAndReleaseCompleted);
		await AssertionExtensions.WaitForGC(creation.BrushReferences.ToArray());

		GC.KeepAlive(sharedStop1);
		GC.KeepAlive(sharedStop2);
		GC.KeepAlive(page);

		[MethodImpl(MethodImplOptions.NoInlining)]
		static (List<WeakReference> BrushReferences, List<LinearGradientBrush> Brushes, List<BoxView> Boxes) CreateBrushes(
			GradientStop sharedStop1,
			GradientStop sharedStop2,
			BoxView previewBox)
		{
			var brushReferences = new List<WeakReference>(BrushCount);
			var brushes = new List<LinearGradientBrush>(BrushCount);
			var boxes = new List<BoxView>(BrushCount);

			for (int i = 0; i < BrushCount; i++)
			{
				var brush = new LinearGradientBrush
				{
					StartPoint = new Point(0, 0),
					EndPoint = new Point(1, 1)
				};
				brush.GradientStops.Add(sharedStop1);
				brush.GradientStops.Add(sharedStop2);

				var box = new BoxView
				{
					HeightRequest = 4,
					Background = brush
				};

				brushReferences.Add(new WeakReference(brush));
				brushes.Add(brush);
				boxes.Add(box);
			}

			previewBox.Background = brushes[0];
			return (brushReferences, brushes, boxes);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void AssertCreationState(
			(List<WeakReference> BrushReferences, List<LinearGradientBrush> Brushes, List<BoxView> Boxes) creation,
			GradientStop sharedStop1,
			GradientStop sharedStop2,
			BoxView previewBox)
		{
			Assert.Equal(BrushCount, creation.Brushes.Count);
			Assert.Equal(BrushCount, creation.Boxes.Count);
			Assert.Equal(BrushCount, creation.BrushReferences.Count);
			for (int i = 0; i < BrushCount; i++)
			{
				Assert.Same(creation.Brushes[i], creation.BrushReferences[i].Target);
				Assert.Collection(
					creation.Brushes[i].GradientStops,
					stop => Assert.Same(sharedStop1, stop),
					stop => Assert.Same(sharedStop2, stop));
				Assert.Same(creation.Brushes[i], creation.Boxes[i].Background);
			}
			Assert.Same(creation.Brushes[0], previewBox.Background);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static bool ClearAndRelease(
			(List<WeakReference> BrushReferences, List<LinearGradientBrush> Brushes, List<BoxView> Boxes) creation,
			BoxView previewBox)
		{
			bool clearAndReleaseCompleted = false;
			foreach (var brush in creation.Brushes)
				brush.GradientStops.Clear();

			int clearedBrushCount = creation.Brushes.Count(brush => brush.GradientStops.Count == 0);
			creation.Brushes.Clear();
			creation.Boxes.Clear();
			previewBox.Background = null;
			clearAndReleaseCompleted =
				clearedBrushCount == BrushCount &&
				creation.Brushes.Count == 0 &&
				creation.Boxes.Count == 0 &&
				previewBox.Background is null;

			return clearAndReleaseCompleted;
		}
	}
}
#endif

