using System;
using System.Collections.Generic;
using CoreAnimation;
using CoreGraphics;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using UIKit;

namespace Microsoft.Maui.Platform
{
	public class ContentView : MauiView
	{
		WeakReference<IBorderStroke>? _clip;
		CAShapeLayer? _contentMask;

		// When the BorderHandler sets the content UIView, it tags it with this so we can 
		// verify we're using the correct subview for masking (and any other purposes)
		internal const nint ContentTag = 0x63D2A0;

		public ContentView()
		{
			if (OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13, 1))
				Layer.CornerCurve = CACornerCurve.Continuous; // Available from iOS 13. More info: https://developer.apple.com/documentation/quartzcore/calayercornercurve/3152600-continuous
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			UpdateClip();
		}

		internal IBorderStroke? Clip
		{
			get => _clip is not null && _clip.TryGetTarget(out var clip) ? clip : null;
			set
			{
				_clip = value is null ? null : new(value);

				if (value is not null)
				{
					UpdateClip();
				}
			}
		}

		UIView? PlatformContent
		{
			get
			{
				// It's a fair bet that Subviews[0] will always be the content for the ContentView
				// But just in case, we're going to iterate over the views and check the tag
				foreach (var subview in Subviews)
				{
					if (subview.Tag == ContentTag)
					{
						return subview;
					}
				}

				return null;
			}
		}

		void RemoveContentMask()
		{
			if (_contentMask is not null && _contentMask.Handle != IntPtr.Zero)
			{
				_contentMask.RemoveFromSuperLayer();
			}
			_contentMask = null;
		}

		void UpdateClip()
		{
			var content = PlatformContent;

			if (Clip is null || Bounds == CGRect.Empty || content == null || content.Frame == CGRect.Empty)
			{
				RemoveContentMask();
				return;
			}

			_contentMask ??= new StaticCAShapeLayer();

			var bounds = Bounds;

			var strokeThickness = (float)Clip.StrokeThickness;

			// We need to inset the content clipping by the width of the stroke on both sides
			// (top and bottom, left and right), so we remove it twice from the total width/height 
			var strokeInset = 2 * strokeThickness;
			var clipWidth = (float)bounds.Width - strokeInset;
			var clipHeight = (float)bounds.Height - strokeInset;

			var clipBounds = new RectF(0, 0, clipWidth, clipHeight);
			_contentMask.Path = GetClipPath(clipBounds, strokeThickness);

			// Since the mask is on the content's CALayer, it's anchored to the content. But we need it to be
			// relative to _this_ container. So we need to compute an adjusted position for it.

			var contentFrame = content.Frame;
			var contentOffsetX = contentFrame.X;
			var contentOffsetY = contentFrame.Y;

			var clipBoundsCenter = clipBounds.Center;
			var clipCenterX = clipBoundsCenter.X + (strokeThickness);
			var clipCenterY = clipBoundsCenter.Y + (strokeThickness);

			CGPoint adjustedMaskPosition = new(clipCenterX - contentOffsetX, clipCenterY - contentOffsetY);

			_contentMask.Bounds = clipBounds;
			_contentMask.Position = adjustedMaskPosition;

			// Set the mask on the content, if it isn't already
			if (content.Layer.Mask != _contentMask)
			{
				content.Layer.Mask = _contentMask;
			}
		}

		CGPath? GetClipPath(RectF bounds, float strokeThickness)
		{
			IShape? clipShape = Clip?.Shape;
			PathF? path;

			if (clipShape is IRoundRectangle roundRectangle)
				path = roundRectangle.InnerPathForBounds(bounds, strokeThickness);
			else
				path = GetInnerPath(clipShape, bounds, strokeThickness);

			return path?.AsCGPath();
		}

		// `bounds` here is the already-shrunk clip bounds (the view bounds minus the stroke thickness on
		// every side). Shapes that build their geometry from the bounds they are handed (Ellipse,
		// Rectangle, ...) are correctly inset by simply asking them for the path of those smaller bounds.
		// Shapes with absolute geometry (Polygon, Polyline, Path) ignore the smaller bounds, so the path
		// would come back unchanged and the mask would end up covering the stroke. For those we build a
		// true inner path by eroding the stroke's own outline.
		static PathF? GetInnerPath(IShape? clipShape, RectF bounds, float strokeThickness)
		{
			if (clipShape is null)
				return null;

			if (strokeThickness > 0)
			{
				// The stroke is drawn using the path of the full view bounds; the clip bounds are that
				// rect deflated by the stroke thickness on all four sides.
				var strokeBounds = new RectF(0, 0, bounds.Width + (2 * strokeThickness), bounds.Height + (2 * strokeThickness));

				if (TryGetClosedPolygon(clipShape.PathForBounds(strokeBounds), out var vertices) &&
					TryInsetPolygon(vertices, strokeThickness, out var insetVertices))
				{
					var innerPath = new PathF();

					// The mask layer's origin sits at (strokeThickness, strokeThickness) in view space.
					innerPath.MoveTo(insetVertices[0].X - strokeThickness, insetVertices[0].Y - strokeThickness);

					for (int i = 1; i < insetVertices.Length; i++)
						innerPath.LineTo(insetVertices[i].X - strokeThickness, insetVertices[i].Y - strokeThickness);

					innerPath.Close();

					return innerPath;
				}
			}

			return clipShape.PathForBounds(bounds);
		}

		// Returns the vertices of `path` when it describes a single closed subpath made up entirely of
		// straight segments; otherwise returns false and the caller falls back to the default behavior.
		static bool TryGetClosedPolygon(PathF? path, out PointF[] vertices)
		{
			vertices = Array.Empty<PointF>();

			if (path is null || path.SubPathCount != 1 || !path.Closed || path.OperationCount < 3)
				return false;

			int pointIndex = 0;
			var points = new List<PointF>();

			foreach (var operation in path.SegmentTypes)
			{
				switch (operation)
				{
					case PathOperation.Move:
					case PathOperation.Line:
						if (pointIndex >= path.Count)
							return false;

						var point = path[pointIndex++];

						if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
							return false;

						// Skip repeated vertices; they produce zero-length edges with no usable normal.
						if (points.Count == 0 || Distance(points[points.Count - 1], point) > Epsilon)
							points.Add(point);
						break;
					case PathOperation.Close:
						break;
					default:
						// Quad/Cubic/Arc segments can't be offset with straight-line math.
						return false;
				}
			}

			// A closed path repeats the start point when the last segment lands back on it.
			if (points.Count > 1 && Distance(points[0], points[points.Count - 1]) <= Epsilon)
				points.RemoveAt(points.Count - 1);

			if (points.Count < 3)
				return false;

			vertices = points.ToArray();
			return true;
		}

		// Offsets every edge of the polygon inwards by `inset` and intersects consecutive offset edges to
		// find the new vertices. Fails (so the caller can fall back) when the polygon collapses.
		static bool TryInsetPolygon(PointF[] vertices, float inset, out PointF[] insetVertices)
		{
			insetVertices = Array.Empty<PointF>();

			var area = SignedArea(vertices);

			if (Math.Abs(area) <= Epsilon)
				return false;

			// The inward normal of the edge (x, y) is (y, -x) for a clockwise polygon and (-y, x) otherwise.
			var direction = area > 0 ? -1f : 1f;
			var result = new PointF[vertices.Length];

			for (int i = 0; i < vertices.Length; i++)
			{
				var previous = vertices[(i + vertices.Length - 1) % vertices.Length];
				var current = vertices[i];
				var next = vertices[(i + 1) % vertices.Length];

				if (!TryOffsetEdge(previous, current, direction * inset, out var a0, out var a1) ||
					!TryOffsetEdge(current, next, direction * inset, out var b0, out var b1) ||
					!TryIntersectLines(a0, a1, b0, b1, out var intersection))
					return false;

				result[i] = intersection;
			}

			var insetArea = SignedArea(result);

			// The eroded polygon has to keep the same winding and be strictly smaller; anything else means
			// the shape collapsed or self-intersected and the mask would be nonsense.
			if (Math.Sign(insetArea) != Math.Sign(area) || Math.Abs(insetArea) >= Math.Abs(area))
				return false;

			insetVertices = result;
			return true;
		}

		static bool TryOffsetEdge(PointF start, PointF end, float offset, out PointF offsetStart, out PointF offsetEnd)
		{
			offsetStart = start;
			offsetEnd = end;

			var dx = end.X - start.X;
			var dy = end.Y - start.Y;
			var length = (float)Math.Sqrt((dx * dx) + (dy * dy));

			if (length <= Epsilon)
				return false;

			var normalX = dy / length * offset;
			var normalY = -dx / length * offset;

			offsetStart = new PointF(start.X + normalX, start.Y + normalY);
			offsetEnd = new PointF(end.X + normalX, end.Y + normalY);
			return true;
		}

		static bool TryIntersectLines(PointF a0, PointF a1, PointF b0, PointF b1, out PointF intersection)
		{
			intersection = default;

			var ax = a1.X - a0.X;
			var ay = a1.Y - a0.Y;
			var bx = b1.X - b0.X;
			var by = b1.Y - b0.Y;

			var denominator = (ax * by) - (ay * bx);

			// Collinear or (near) parallel edges have no usable miter point.
			if (Math.Abs(denominator) <= Epsilon)
				return false;

			var t = (((b0.X - a0.X) * by) - ((b0.Y - a0.Y) * bx)) / denominator;
			var x = a0.X + (ax * t);
			var y = a0.Y + (ay * t);

			if (!float.IsFinite(x) || !float.IsFinite(y))
				return false;

			intersection = new PointF(x, y);
			return true;
		}

		static float SignedArea(PointF[] vertices)
		{
			float area = 0;

			for (int i = 0; i < vertices.Length; i++)
			{
				var current = vertices[i];
				var next = vertices[(i + 1) % vertices.Length];
				area += (current.X * next.Y) - (next.X * current.Y);
			}

			return area / 2;
		}

		static float Distance(PointF a, PointF b)
		{
			var dx = a.X - b.X;
			var dy = a.Y - b.Y;
			return (float)Math.Sqrt((dx * dx) + (dy * dy));
		}

		const float Epsilon = 0.0001f;

		public override void WillRemoveSubview(UIView uiview)
		{
			// Make sure we're not holding a mask for content we no longer own
			if (uiview == PlatformContent)
			{
				RemoveContentMask();
			}

			base.WillRemoveSubview(uiview);
		}
	}
}
