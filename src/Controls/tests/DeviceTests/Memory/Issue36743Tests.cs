#if ANDROID
using System;
using System.Collections.Generic;
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
		var sharedStart = new GradientStop(Colors.DeepSkyBlue, 0f);
		var sharedEnd = new GradientStop(Colors.MediumVioletRed, 1f);

		var brushReferences = CreateAndReleaseBrushes(sharedStart, sharedEnd);

		Assert.Equal(BrushCount, brushReferences.Length);
		await AssertionExtensions.WaitForGC(brushReferences);

		GC.KeepAlive(sharedStart);
		GC.KeepAlive(sharedEnd);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static WeakReference[] CreateAndReleaseBrushes(GradientStop sharedStart, GradientStop sharedEnd)
	{
		var brushes = new List<LinearGradientBrush>(BrushCount);
		var boxes = new List<BoxView>(BrushCount);
		var brushReferences = new WeakReference[BrushCount];
		var preview = new BoxView
		{
			HeightRequest = 60,
			CornerRadius = 8,
		};

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

		preview.Background = brushes[0];

		Assert.Equal(BrushCount, brushes.Count);
		Assert.Equal(BrushCount, boxes.Count);
		Assert.Equal(60d, preview.HeightRequest);
		Assert.Equal(new CornerRadius(8), preview.CornerRadius);
		Assert.Same(brushes[0], preview.Background);

		for (int i = 0; i < BrushCount; i++)
		{
			Assert.Equal(new Point(0, 0), brushes[i].StartPoint);
			Assert.Equal(new Point(1, 1), brushes[i].EndPoint);
			Assert.Equal(2, brushes[i].GradientStops.Count);
			Assert.Same(sharedStart, brushes[i].GradientStops[0]);
			Assert.Same(sharedEnd, brushes[i].GradientStops[1]);
			Assert.Equal(4d, boxes[i].HeightRequest);
			Assert.Same(brushes[i], boxes[i].Background);
		}

		int clearedCount = -1;
		Assert.Equal(-1, clearedCount);
		clearedCount = 0;
		foreach (var brush in brushes)
		{
			brush.GradientStops.Clear();
			clearedCount++;
		}

		Assert.Equal(BrushCount, clearedCount);
		Assert.All(brushes, brush => Assert.Empty(brush.GradientStops));

		preview.Background = null;
		brushes.Clear();
		boxes.Clear();

		Assert.Null(preview.Background);
		Assert.Empty(brushes);
		Assert.Empty(boxes);

		return brushReferences;
	}
}
#endif

