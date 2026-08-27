#if IOS && !MACCATALYST
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.ImageAnalysis;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue27866")]
	public class Issue27866 : ControlsHandlerTestBase
	{
		const string HtmlList = "<ul><li>item 1</li><li>item 2</li><li>item 3</li></ul><ol><li>item 1</li><li>item 2</li><li>item 3</li></ol>";
		const string ItemOnlyHtml = "item 1<br>item 2<br>item 3<br>item 1<br>item 2<br>item 3";

		[Fact]
		public async Task HtmlListsRenderUnorderedAndOrderedMarkers()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var itemOnlyBitmap = await RenderAffectedLabel(TextType.Html, ItemOnlyHtml);
			var htmlBitmap = await RenderAffectedLabel(TextType.Html, HtmlList);
			var itemOnlyRows = FindInkRows(itemOnlyBitmap);
			var htmlRows = FindInkRows(htmlBitmap);

			Assert.Equal(6, itemOnlyRows.Count);
			Assert.Equal(6, htmlRows.Count);

			var missingMarkers = new List<string>();
			for (int rowIndex = 0; rowIndex < htmlRows.Count; rowIndex++)
			{
				var itemWidth = itemOnlyRows[rowIndex].Right - itemOnlyRows[rowIndex].Left + 1;
				var htmlWidth = htmlRows[rowIndex].Right - htmlRows[rowIndex].Left + 1;
				if (htmlWidth <= itemWidth)
				{
					var listKind = rowIndex < 3 ? "unordered" : "ordered";
					missingMarkers.Add(
						$"{listKind} row {rowIndex + 1}: rendered width {htmlWidth}, item-only width {itemWidth}");
				}
			}

			Assert.True(missingMarkers.Count == 0,
				$"HTML list markers were not rendered: {string.Join("; ", missingMarkers)}");
		}

		async Task<RawBitmap> RenderAffectedLabel(TextType textType, string text)
		{
			var affectedLabel = new Label
			{
				HorizontalOptions = LayoutOptions.Center,
				Text = text,
				TextType = textType,
			};
			var layout = new VerticalStackLayout
			{
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Spacing = 24,
				Children =
				{
					affectedLabel,
					new Label
					{
						HorizontalOptions = LayoutOptions.Center,
						Text = "List marker diagnostics",
					},
					new Button
					{
						HorizontalOptions = LayoutOptions.Center,
						Text = "Check list markers",
					},
				},
			};
			var page = new ContentPage
			{
				Title = "Home",
				Content = layout,
			};
			RawBitmap bitmap = null;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(
					() => affectedLabel.Width > 0 && affectedLabel.Height > 0,
					message: "The affected label did not complete layout.");
				bitmap = await affectedLabel.AsRawBitmapAsync();
			});

			Assert.NotNull(bitmap);
			return bitmap;
		}

		static List<(int Top, int Bottom, int Left, int Right)> FindInkRows(RawBitmap bitmap)
		{
			var activeRows = new List<int>();
			for (int y = 0; y < bitmap.PixelHeight; y++)
			{
				for (int x = 0; x < bitmap.PixelWidth; x++)
				{
					if (IsInk(bitmap, x, y))
					{
						activeRows.Add(y);
						break;
					}
				}
			}

			var ranges = new List<(int Top, int Bottom)>();
			foreach (var y in activeRows)
			{
				if (ranges.Count == 0 || y - ranges[^1].Bottom > 2)
					ranges.Add((y, y));
				else
					ranges[^1] = (ranges[^1].Top, y);
			}

			var rows = new List<(int Top, int Bottom, int Left, int Right)>();
			foreach (var range in ranges)
			{
				var left = bitmap.PixelWidth;
				var right = -1;
				for (int y = range.Top; y <= range.Bottom; y++)
				{
					for (int x = 0; x < bitmap.PixelWidth; x++)
					{
						if (!IsInk(bitmap, x, y))
							continue;

						left = Math.Min(left, x);
						right = Math.Max(right, x);
					}
				}

				rows.Add((range.Top, range.Bottom, left, right));
			}

			return rows;
		}

		static bool IsInk(RawBitmap bitmap, int x, int y)
		{
			var index = (y * bitmap.PixelWidth + x) * 4;
			return Math.Abs(bitmap.PixelBuffer[index] - bitmap.PixelBuffer[0]) > 40 ||
				Math.Abs(bitmap.PixelBuffer[index + 1] - bitmap.PixelBuffer[1]) > 40 ||
				Math.Abs(bitmap.PixelBuffer[index + 2] - bitmap.PixelBuffer[2]) > 40 ||
				Math.Abs(bitmap.PixelBuffer[index + 3] - bitmap.PixelBuffer[3]) > 40;
		}
	}
}
#endif

