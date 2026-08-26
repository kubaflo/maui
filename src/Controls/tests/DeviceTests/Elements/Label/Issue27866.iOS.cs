#if IOS && !MACCATALYST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue27866")]
	public class Issue27866 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HtmlListsRenderMarkersOnEveryItem()
		{
			if (!OperatingSystem.IsIOSVersionAtLeast(18))
				return;

			string[] items = ["item 1", "item 2", "item 3"];

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var unorderedReference = CreateLabel("•    item 1\n•    item 2\n•    item 3");
			var unorderedPlainReference = CreateLabel("item 1\nitem 2\nitem 3");
			var orderedReference = CreateLabel("1.    item 1\n2.    item 2\n3.    item 3");
			var orderedPlainReference = CreateLabel("item 1\nitem 2\nitem 3");
			var htmlUnordered = CreateHtmlLabel("<ul><li>item 1</li><li>item 2</li><li>item 3</li></ul>");
			var htmlOrdered = CreateHtmlLabel("<ol><li>item 1</li><li>item 2</li><li>item 3</li></ol>");
			var htmlPlain = CreateHtmlLabel("item 1<br>item 2<br>item 3");

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					unorderedReference,
					unorderedPlainReference,
					orderedReference,
					orderedPlainReference,
					htmlUnordered,
					htmlOrdered,
					htmlPlain,
				}
			};
			var page = new ContentPage
			{
				Content = new ScrollView { Content = layout }
			};

			bool loadedObserved = false;
			int layoutPasses = -1;
			page.Loaded += (_, _) => loadedObserved = true;
			layout.SizeChanged += (_, _) => layoutPasses = layoutPasses < 0 ? 1 : layoutPasses + 1;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertHelpers.AssertEventually(
					() => loadedObserved,
					timeout: 5000,
					message: "The page did not report Loaded after window attachment.");
				await AssertHelpers.AssertEventually(
					() => layoutPasses > 0,
					timeout: 5000,
					message: "The attached layout did not report a completed size transition.");

				var unorderedReferenceView = GetPlatformLabel(unorderedReference);
				var unorderedPlainReferenceView = GetPlatformLabel(unorderedPlainReference);
				var orderedReferenceView = GetPlatformLabel(orderedReference);
				var orderedPlainReferenceView = GetPlatformLabel(orderedPlainReference);
				var htmlUnorderedView = GetPlatformLabel(htmlUnordered);
				var htmlOrderedView = GetPlatformLabel(htmlOrdered);
				var htmlPlainView = GetPlatformLabel(htmlPlain);
				var platformLabels = new[]
				{
					unorderedReferenceView,
					unorderedPlainReferenceView,
					orderedReferenceView,
					orderedPlainReferenceView,
					htmlUnorderedView,
					htmlOrderedView,
					htmlPlainView,
				};

				await AssertHelpers.AssertEventually(
					() => platformLabels.All(label => label.Frame.Width > 0 && label.Frame.Height > 0),
					timeout: 5000,
					message: "One or more native labels did not receive a nonempty frame.");
				await AssertHelpers.AssertEventually(
					() => ContainsAllItems(htmlUnorderedView, items) &&
						ContainsAllItems(htmlOrderedView, items) &&
						ContainsAllItems(htmlPlainView, items),
					timeout: 5000,
					message: "The asynchronous HTML conversion did not contain every list item.");

				var analysis = await InvokeOnMainThreadAsync(() =>
				{
					var unorderedReferenceBitmap = Render(unorderedReferenceView);
					var unorderedPlainReferenceBitmap = Render(unorderedPlainReferenceView);
					var orderedReferenceBitmap = Render(orderedReferenceView);
					var orderedPlainReferenceBitmap = Render(orderedPlainReferenceView);
					var htmlUnorderedBitmap = Render(htmlUnorderedView);
					var htmlOrderedBitmap = Render(htmlOrderedView);
					var htmlPlainBitmap = Render(htmlPlainView);

					int unorderedMarkerWidth = unorderedReferenceBitmap.Width - unorderedPlainReferenceBitmap.Width;
					int orderedMarkerWidth = orderedReferenceBitmap.Width - orderedPlainReferenceBitmap.Width;
					Assert.True(unorderedMarkerWidth > 0, "The unordered marked reference must be wider than its plain reference.");
					Assert.True(orderedMarkerWidth > 0, "The ordered marked reference must be wider than its plain reference.");

					return new MarkerAnalysis(
						DetectMarkers(unorderedReferenceBitmap, unorderedPlainReferenceBitmap, unorderedMarkerWidth),
						DetectMarkers(unorderedPlainReferenceBitmap, unorderedPlainReferenceBitmap, unorderedMarkerWidth),
						DetectMarkers(orderedReferenceBitmap, orderedPlainReferenceBitmap, orderedMarkerWidth),
						DetectMarkers(orderedPlainReferenceBitmap, orderedPlainReferenceBitmap, orderedMarkerWidth),
						DetectMarkers(htmlUnorderedBitmap, htmlPlainBitmap, unorderedMarkerWidth),
						DetectMarkers(htmlOrderedBitmap, htmlPlainBitmap, orderedMarkerWidth));
				});

				Assert.Equal(items.Length, analysis.UnorderedReference.LineCount);
				Assert.Equal(items.Length, analysis.OrderedReference.LineCount);
				Assert.Equal(items.Length, analysis.HtmlUnordered.LineCount);
				Assert.Equal(items.Length, analysis.HtmlOrdered.LineCount);
				Assert.Equal(items.Length, analysis.UnorderedReference.MarkerRows);
				Assert.Equal(0, analysis.UnorderedPlain.MarkerRows);
				Assert.Equal(items.Length, analysis.OrderedReference.MarkerRows);
				Assert.Equal(0, analysis.OrderedPlain.MarkerRows);

				Assert.True(
					analysis.HtmlUnordered.MarkerRows == items.Length,
					$"HTML unordered list marker rows: observed {analysis.HtmlUnordered.MarkerRows}, expected {items.Length}; frame={htmlUnorderedView.Frame}; region={analysis.HtmlUnordered.Region}.");
				Assert.True(
					analysis.HtmlOrdered.MarkerRows == items.Length,
					$"HTML ordered list marker rows: observed {analysis.HtmlOrdered.MarkerRows}, expected {items.Length}; frame={htmlOrderedView.Frame}; region={analysis.HtmlOrdered.Region}.");
			});
		}

		static Label CreateLabel(string text) =>
			new Label
			{
				HorizontalOptions = LayoutOptions.Center,
				Text = text
			};

		static Label CreateHtmlLabel(string text) =>
			new Label
			{
				HorizontalOptions = LayoutOptions.Center,
				TextType = TextType.Html,
				Text = text
			};

		static UILabel GetPlatformLabel(Label label)
		{
			Assert.NotNull(label.Handler);
			var platformLabel = label.Handler.PlatformView as UILabel;
			Assert.NotNull(platformLabel);
			return platformLabel;
		}

		static bool ContainsAllItems(UILabel label, string[] items)
		{
			var value = label.AttributedText?.Value;
			return value is not null && items.All(value.Contains);
		}

		static RenderedLabel Render(UILabel label)
		{
			using var format = new UIGraphicsImageRendererFormat
			{
				Opaque = false,
				Scale = 1
			};
			using var renderer = new UIGraphicsImageRenderer(label.Bounds.Size, format);
			using var image = renderer.CreateImage(context => label.Layer.RenderInContext(context.CGContext));
			var cgImage = image.CGImage;
			Assert.NotNull(cgImage);
			Assert.True(
				cgImage.ByteOrderInfo == CGImageByteOrderInfo.ByteOrder32Little && cgImage.BitsPerPixel == 32,
				$"Unexpected rendered pixel format: byteOrder={cgImage.ByteOrderInfo}, bitsPerPixel={cgImage.BitsPerPixel}.");
			using var data = cgImage.DataProvider.CopyData();
			return new RenderedLabel
			{
				Width = (int)cgImage.Width,
				Height = (int)cgImage.Height,
				BytesPerRow = (int)cgImage.BytesPerRow,
				Pixels = data.ToArray()
			};
		}

		static MarkerDetection DetectMarkers(RenderedLabel target, RenderedLabel plain, int markerWidth)
		{
			var bands = GetLineBands(target);
			int canvasWidth = Math.Max(target.Width, plain.Width + markerWidth);
			int itemStart = canvasWidth - plain.Width;
			int markerStart = Math.Max(0, itemStart - markerWidth);
			int targetOffset = canvasWidth - target.Width;
			int markerRows = 0;

			foreach (var band in bands)
			{
				bool hasMarker = false;
				for (int y = band.Start; y <= band.End && !hasMarker; y++)
				{
					for (int canvasX = markerStart; canvasX < itemStart; canvasX++)
					{
						int sourceX = canvasX - targetOffset;
						if (sourceX >= 0 && sourceX < target.Width && target.AlphaAt(sourceX, y) > 8)
						{
							hasMarker = true;
							break;
						}
					}
				}

				if (hasMarker)
					markerRows++;
			}

			return new MarkerDetection(
				markerRows,
				bands.Count,
				$"x=[{markerStart},{itemStart}), canvasWidth={canvasWidth}, targetOffset={targetOffset}");
		}

		static List<LineBand> GetLineBands(RenderedLabel bitmap)
		{
			var bands = new List<LineBand>();
			int start = -1;

			for (int y = 0; y < bitmap.Height; y++)
			{
				bool rowHasInk = false;
				for (int x = 0; x < bitmap.Width; x++)
				{
					if (bitmap.AlphaAt(x, y) > 8)
					{
						rowHasInk = true;
						break;
					}
				}

				if (rowHasInk && start < 0)
					start = y;
				else if (!rowHasInk && start >= 0)
				{
					bands.Add(new LineBand(start, y - 1));
					start = -1;
				}
			}

			if (start >= 0)
				bands.Add(new LineBand(start, bitmap.Height - 1));

			return bands;
		}

		readonly record struct RenderedLabel
		{
			public int Width { get; init; }
			public int Height { get; init; }
			public int BytesPerRow { get; init; }
			public byte[] Pixels { get; init; }

			public byte AlphaAt(int x, int y) => Pixels[(y * BytesPerRow) + (x * 4) + 3];
		}

		readonly record struct LineBand(int Start, int End);
		readonly record struct MarkerDetection(int MarkerRows, int LineCount, string Region);
		readonly record struct MarkerAnalysis(
			MarkerDetection UnorderedReference,
			MarkerDetection UnorderedPlain,
			MarkerDetection OrderedReference,
			MarkerDetection OrderedPlain,
			MarkerDetection HtmlUnordered,
			MarkerDetection HtmlOrdered);
	}
}
#endif

