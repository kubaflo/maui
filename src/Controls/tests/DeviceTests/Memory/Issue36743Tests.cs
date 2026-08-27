#if ANDROID
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
	public async Task ClearingSharedGradientStopsReleasesBrushes()
	{
		var (brushReferences, sharedStops) = CreateClearAndReleaseBrushes();

		Assert.Equal(BrushCount, brushReferences.Length);
		Assert.Equal(2, sharedStops.Length);

		await AssertionExtensions.WaitForGC(brushReferences);

		GC.KeepAlive(sharedStops);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static (WeakReference[] BrushReferences, GradientStop[] SharedStops) CreateClearAndReleaseBrushes()
	{
		var sharedStop1 = new GradientStop(Colors.DeepSkyBlue, 0f);
		var sharedStop2 = new GradientStop(Colors.MediumVioletRed, 1f);
		var sharedStops = new[] { sharedStop1, sharedStop2 };
		var brushes = new List<LinearGradientBrush>(BrushCount);
		var boxes = new List<BoxView>(BrushCount);
		var brushReferences = new List<WeakReference>(BrushCount);
		var preview = new BoxView
		{
			HeightRequest = 60,
			CornerRadius = 8
		};
		int observedResets = -1;

		for (int i = 0; i < BrushCount; i++)
		{
			var brush = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 1)
			};
			brush.GradientStops.Add(sharedStop1);
			brush.GradientStops.Add(sharedStop2);
			brush.GradientStops.CollectionChanged += OnGradientStopsChanged;

			var box = new BoxView
			{
				HeightRequest = 4,
				Background = brush
			};

			brushes.Add(brush);
			boxes.Add(box);
			brushReferences.Add(new WeakReference(brush));
		}

		preview.Background = brushes[0];

		Assert.Equal(BrushCount, brushes.Count);
		Assert.Equal(BrushCount, boxes.Count);
		Assert.Equal(BrushCount, brushReferences.Count);
		for (int i = 0; i < BrushCount; i++)
		{
			Assert.Equal(new Point(0, 0), brushes[i].StartPoint);
			Assert.Equal(new Point(1, 1), brushes[i].EndPoint);
			Assert.Equal(2, brushes[i].GradientStops.Count);
			Assert.Same(sharedStop1, brushes[i].GradientStops[0]);
			Assert.Same(sharedStop2, brushes[i].GradientStops[1]);
			Assert.Same(brushes[i], boxes[i].Background);
			Assert.Same(brushes[i], brushReferences[i].Target);
		}
		Assert.Same(brushes[0], preview.Background);

		observedResets = 0;
		foreach (var brush in brushes)
			brush.GradientStops.Clear();

		Assert.NotEqual(-1, observedResets);
		Assert.Equal(BrushCount, observedResets);
		foreach (var brush in brushes)
			Assert.Empty(brush.GradientStops);

		preview.Background = null;
		return (brushReferences.ToArray(), sharedStops);

		void OnGradientStopsChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			if (args.Action == NotifyCollectionChangedAction.Reset)
				observedResets++;
		}
	}
}
#endif

