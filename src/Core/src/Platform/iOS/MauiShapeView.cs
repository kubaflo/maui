using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using CoreGraphics;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Platform
{
	public class MauiShapeView : PlatformGraphicsView, IUIViewLifeCycleEvents
	{
		// Flattening tolerance (in points) used when converting curves to line segments for hit testing.
		// Sub-pixel accuracy is more than enough for a touch test and keeps the segment count small.
		const float HitTestFlatness = 0.5f;

		// Minimum width of the stroke hit region so that hairline strokes stay tappable.
		const float MinimumStrokeHitWidth = 1f;

		public MauiShapeView()
		{
			BackgroundColor = UIColor.Clear;
		}

		/// <summary>
		/// Restricts hit testing to the geometry that is actually rendered instead of the whole
		/// rectangular bounds of the view, so a shape that only paints a small part of its layout
		/// rect (a <c>Line</c>, for example) no longer swallows touches meant for the views beneath it.
		/// </summary>
		public override bool PointInside(CGPoint point, UIEvent? uievent)
		{
			if (!base.PointInside(point, uievent))
				return false;

			// If the drawn geometry cannot be determined, fall back to the rectangular hit test so a
			// shape is never made untappable by this check.
			if (Drawable is not ShapeDrawable shapeDrawable)
				return true;

			var shapeView = shapeDrawable.ShapeView;
			var shape = shapeView?.Shape;

			if (shapeView is null || shape is null)
				return true;

			var bounds = Bounds;

			if (bounds.Width <= 0 || bounds.Height <= 0)
				return true;

			PathF? path;

			try
			{
				path = shape.PathForBounds(new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height));
			}
			catch
			{
				return true;
			}

			if (path is null || path.Count == 0)
				return true;

			if (shapeDrawable.RenderTransform is Matrix3x2 renderTransform)
			{
				path = new PathF(path);
				path.Transform(renderTransform);
			}

			var strokeWidth = shapeView.Stroke is null ? 0f : (float)shapeView.StrokeThickness;
			var strokeTolerance = Math.Max(strokeWidth, MinimumStrokeHitWidth) / 2f;

			return ShapeContainsPoint(
				path,
				new PointF((float)point.X, (float)point.Y),
				shapeDrawable.WindingMode == WindingMode.EvenOdd,
				strokeTolerance);
		}

		static bool ShapeContainsPoint(PathF path, PointF point, bool evenOdd, float strokeTolerance)
		{
			var subPaths = GetFlattenedSubPaths(path);

			if (subPaths.Count == 0)
				return true;

			var winding = 0;
			var crossings = 0;

			foreach (var subPath in subPaths)
			{
				var points = subPath.Points;

				if (points.Count == 0)
					continue;

				for (var i = 0; i < points.Count - 1; i++)
				{
					// The outline is painted when a stroke is present, so a point close enough to any
					// rendered segment is a hit regardless of whether the shape is filled.
					if (DistanceToSegment(point, points[i], points[i + 1]) <= strokeTolerance)
						return true;

					AccumulateFillCrossings(points[i], points[i + 1], point, ref winding, ref crossings);
				}

				var first = points[0];
				var last = points[points.Count - 1];

				if (subPath.Closed && points.Count > 1 && DistanceToSegment(point, last, first) <= strokeTolerance)
					return true;

				// Filling always treats a sub-path as closed, so the implicit closing edge takes part in
				// the fill test even when the sub-path was left open.
				if (points.Count > 2)
					AccumulateFillCrossings(last, first, point, ref winding, ref crossings);
			}

			return evenOdd ? (crossings & 1) == 1 : winding != 0;
		}

		static List<SubPath> GetFlattenedSubPaths(PathF path)
		{
			var flattened = path.GetFlattenedPath(HitTestFlatness, includeSubPaths: true);
			var subPaths = new List<SubPath>();
			SubPath? current = null;
			var pointIndex = 0;

			foreach (var operation in flattened.SegmentTypes)
			{
				switch (operation)
				{
					case PathOperation.Move:
						if (pointIndex >= flattened.Count)
							return subPaths;

						current = new SubPath();
						current.Points.Add(flattened[pointIndex++]);
						subPaths.Add(current);
						break;

					case PathOperation.Line:
						if (pointIndex >= flattened.Count)
							return subPaths;

						if (current is null)
						{
							current = new SubPath();
							subPaths.Add(current);
						}

						current.Points.Add(flattened[pointIndex++]);
						break;

					case PathOperation.Close:
						if (current is not null)
						{
							current.Closed = true;
							current = null;
						}
						break;

					default:
						// GetFlattenedPath only emits Move/Line/Close; anything else means the path could
						// not be flattened as expected, so give up and use the rectangular hit test.
						return new List<SubPath>();
				}
			}

			return subPaths;
		}

		// Winding number and even-odd crossing accumulation for the standard horizontal ray cast.
		static void AccumulateFillCrossings(PointF start, PointF end, PointF point, ref int winding, ref int crossings)
		{
			if (start.Y <= point.Y)
			{
				if (end.Y > point.Y && IsLeft(start, end, point) > 0)
					winding++;
			}
			else if (end.Y <= point.Y && IsLeft(start, end, point) < 0)
			{
				winding--;
			}

			if ((start.Y > point.Y) != (end.Y > point.Y))
			{
				var t = (point.Y - start.Y) / (end.Y - start.Y);

				if (start.X + (t * (end.X - start.X)) > point.X)
					crossings++;
			}
		}

		static float IsLeft(PointF start, PointF end, PointF point) =>
			((end.X - start.X) * (point.Y - start.Y)) - ((point.X - start.X) * (end.Y - start.Y));

		static float DistanceToSegment(PointF point, PointF start, PointF end)
		{
			var dx = end.X - start.X;
			var dy = end.Y - start.Y;
			var lengthSquared = (dx * dx) + (dy * dy);

			float closestX;
			float closestY;

			if (lengthSquared <= float.Epsilon)
			{
				closestX = start.X;
				closestY = start.Y;
			}
			else
			{
				var t = (((point.X - start.X) * dx) + ((point.Y - start.Y) * dy)) / lengthSquared;
				t = Math.Clamp(t, 0f, 1f);
				closestX = start.X + (t * dx);
				closestY = start.Y + (t * dy);
			}

			var offsetX = point.X - closestX;
			var offsetY = point.Y - closestY;

			return (float)Math.Sqrt((offsetX * offsetX) + (offsetY * offsetY));
		}

		sealed class SubPath
		{
			public List<PointF> Points { get; } = new List<PointF>();

			public bool Closed { get; set; }
		}

		[UnconditionalSuppressMessage("Memory", "MEM0002", Justification = IUIViewLifeCycleEvents.UnconditionalSuppressMessage)]
		EventHandler? _movedToWindow;
		event EventHandler IUIViewLifeCycleEvents.MovedToWindow
		{
			add => _movedToWindow += value;
			remove => _movedToWindow -= value;
		}

		public override void MovedToWindow()
		{
			base.MovedToWindow();
			_movedToWindow?.Invoke(this, EventArgs.Empty);
		}
	}
}
