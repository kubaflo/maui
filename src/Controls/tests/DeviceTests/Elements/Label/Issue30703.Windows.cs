#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30703")]
	public class Issue30703 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RotatedLabelsUseIntrinsicTextWidthInsideFixedGridColumns()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandler>();
				});
			});

			const string labelText = "This as a long text";
			var grid = new Grid
			{
				ColumnDefinitions = new ColumnDefinitionCollection(
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(40),
					new ColumnDefinition(80)),
				RowDefinitions = new RowDefinitionCollection(
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto))
			};

			var topWideCell = AddCell(grid, new BoxView { BackgroundColor = Colors.GreenYellow }, 0, 0);
			AddCell(grid, new BoxView { BackgroundColor = Colors.Cyan }, 0, 1);
			AddCell(grid, new BoxView { BackgroundColor = Colors.Beige }, 0, 2);
			AddCell(grid, new BoxView { BackgroundColor = Colors.GreenYellow }, 1, 0);
			var referenceFortyCell = AddCell(grid, new BoxView { BackgroundColor = Colors.Cyan }, 1, 1);
			var referenceEightyCell = AddCell(grid, new BoxView { BackgroundColor = Colors.Beige }, 1, 2);

			var rotatedWide = AddCell(grid, CreateLabel(labelText, 90), 0, 0);
			var rotatedForty = AddCell(grid, CreateLabel(labelText, 90), 0, 1);
			var rotatedEighty = AddCell(grid, CreateLabel(labelText, 90), 0, 2);
			var referenceLabel = AddCell(grid, CreateLabel(labelText, 0), 1, 0);
			AddCell(grid, CreateLabel(labelText, 0), 1, 1);
			AddCell(grid, CreateLabel(labelText, 0), 1, 2);

			var supportingLabel = AddCell(grid, new Label
			{
				Text = "Label rotation",
				HorizontalTextAlignment = TextAlignment.Center,
				Padding = 8
			}, 2, 0);
			Grid.SetColumnSpan(supportingLabel, 3);

			var checkButton = AddCell(grid, new Button { Text = "Check rotated labels" }, 3, 0);
			Grid.SetColumnSpan(checkButton, 3);

			var loadedCount = 0;
			grid.Loaded += (_, _) => loadedCount++;
			var page = new ContentPage { Content = grid };

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await AssertEventually(
					() => loadedCount > 0 &&
						topWideCell.Handler is BoxViewHandler topCellHandler &&
						topCellHandler.PlatformView.ActualHeight > 0 &&
						referenceLabel.Handler is LabelHandler referenceHandler &&
						referenceHandler.PlatformView.ActualWidth > 0,
					message: "Issue30703 Grid did not reach a loaded, nonzero native layout.");

				Assert.True(loadedCount > 0, "Issue30703 expected a post-attachment Loaded callback.");

				var referenceHandler = GetLabelHandler(referenceLabel);
				var rotatedWideHandler = GetLabelHandler(rotatedWide);
				var rotatedFortyHandler = GetLabelHandler(rotatedForty);
				var rotatedEightyHandler = GetLabelHandler(rotatedEighty);

				AssertLabelIdentity(referenceLabel, referenceHandler, labelText, 1, 0);
				AssertLabelIdentity(rotatedWide, rotatedWideHandler, labelText, 0, 0);
				AssertLabelIdentity(rotatedForty, rotatedFortyHandler, labelText, 0, 1);
				AssertLabelIdentity(rotatedEighty, rotatedEightyHandler, labelText, 0, 2);
				Assert.Equal(90, rotatedWide.Rotation);
				Assert.Equal(90, rotatedForty.Rotation);
				Assert.Equal(90, rotatedEighty.Rotation);

				var fortyCellNative = GetBoxViewHandler(referenceFortyCell).PlatformView;
				var eightyCellNative = GetBoxViewHandler(referenceEightyCell).PlatformView;
				Assert.InRange(fortyCellNative.ActualWidth, 39.5, 40.5);
				Assert.InRange(eightyCellNative.ActualWidth, 79.5, 80.5);

				var expectedWidth = referenceHandler.PlatformView.ActualWidth;
				Assert.True(expectedWidth > 80, $"Issue30703 expected intrinsic text width above the fixed columns, measured {expectedWidth:F2}.");

				var topCellNative = GetBoxViewHandler(topWideCell).PlatformView;
				Assert.True(topCellNative.ActualHeight > expectedWidth,
					$"Issue30703 expected the first row height {topCellNative.ActualHeight:F2} to exceed intrinsic text width {expectedWidth:F2}.");

				AssertIntrinsicWidth(rotatedFortyHandler, expectedWidth, "40");
				AssertIntrinsicWidth(rotatedEightyHandler, expectedWidth, "80");
			});
		}

		static Label CreateLabel(string text, double rotation) => new()
		{
			Text = text,
			BackgroundColor = Colors.Orange,
			Rotation = rotation,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};

		static T AddCell<T>(Grid grid, T view, int row, int column)
			where T : View
		{
			Grid.SetRow(view, row);
			Grid.SetColumn(view, column);
			grid.Add(view);
			return view;
		}

		static LabelHandler GetLabelHandler(Label label)
		{
			var handler = label.Handler as LabelHandler;
			Assert.NotNull(handler);
			Assert.NotNull(handler.PlatformView);
			return handler;
		}

		static BoxViewHandler GetBoxViewHandler(BoxView boxView)
		{
			var handler = boxView.Handler as BoxViewHandler;
			Assert.NotNull(handler);
			Assert.NotNull(handler.PlatformView);
			return handler;
		}

		static void AssertLabelIdentity(
			Label label,
			LabelHandler handler,
			string expectedText,
			int expectedRow,
			int expectedColumn)
		{
			Assert.Equal(expectedText, handler.PlatformView.Text);
			Assert.Equal(expectedRow, Grid.GetRow(label));
			Assert.Equal(expectedColumn, Grid.GetColumn(label));
		}

		static void AssertIntrinsicWidth(
			LabelHandler handler,
			double expectedWidth,
			string columnWidth)
		{
			var nativeLabel = handler.PlatformView;
			Assert.True(Math.Abs(nativeLabel.ActualWidth - expectedWidth) <= 0.5,
				$"Issue30703 rotated label native width was clipped: column {columnWidth}, measured {nativeLabel.ActualWidth:F2}, expected {expectedWidth:F2}.");
		}
	}
}
#endif

