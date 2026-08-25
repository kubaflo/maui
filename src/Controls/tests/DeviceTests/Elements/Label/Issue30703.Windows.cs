#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using TextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30703")]
	public class Issue30703 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RotatedLabelPreservesIntrinsicTextWidthInNarrowGridColumn()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			const string labelText = "This as a long text";
			const double tolerance = 1;

			var wideRotatedLabel = new Label
			{
				Text = labelText,
				BackgroundColor = Colors.Orange,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Rotation = -90
			};
			var narrowRotatedLabel = new Label
			{
				Text = labelText,
				BackgroundColor = Colors.Orange,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Rotation = -90
			};
			var mediumRotatedLabel = new Label
			{
				Text = labelText,
				BackgroundColor = Colors.Orange,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Rotation = -90
			};

			var wideRotatedContainer = new Grid { BackgroundColor = Colors.GreenYellow };
			wideRotatedContainer.Add(wideRotatedLabel);
			var narrowRotatedContainer = new Grid { BackgroundColor = Colors.Cyan };
			narrowRotatedContainer.Add(narrowRotatedLabel);
			var mediumRotatedContainer = new Grid { BackgroundColor = Colors.Beige };
			mediumRotatedContainer.Add(mediumRotatedLabel);

			var wideLabel = new Label
			{
				Text = labelText,
				BackgroundColor = Colors.Orange,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			var narrowLabel = new Label
			{
				Text = labelText,
				BackgroundColor = Colors.Orange,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			var mediumLabel = new Label
			{
				Text = labelText,
				BackgroundColor = Colors.Orange,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};

			var wideContainer = new Grid { BackgroundColor = Colors.GreenYellow };
			wideContainer.Add(wideLabel);
			var narrowContainer = new Grid { BackgroundColor = Colors.Cyan };
			narrowContainer.Add(narrowLabel);
			var mediumContainer = new Grid { BackgroundColor = Colors.Beige };
			mediumContainer.Add(mediumLabel);

			var rootGrid = new Grid();
			rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
			rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
			rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
			rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
			rootGrid.Add(wideRotatedContainer, 0, 0);
			rootGrid.Add(narrowRotatedContainer, 1, 0);
			rootGrid.Add(mediumRotatedContainer, 2, 0);
			rootGrid.Add(wideContainer, 0, 1);
			rootGrid.Add(narrowContainer, 1, 1);
			rootGrid.Add(mediumContainer, 2, 1);

			var page = new ContentPage { Content = rootGrid };

			bool attachmentCallbackObserved = false;
			bool handlersObserved = false;
			bool nativeViewsObserved = false;
			bool nativeAttachmentObserved = false;
			bool arrangedTextObserved = false;
			bool nonzeroNativeFramesObserved = false;
			double intrinsicTextWidth = -1;
			double wideNativeWidth = -1;
			double narrowNativeWidth = -1;
			double topRowHeight = -1;
			double narrowColumnWidth = -1;
			double mediumColumnWidth = -1;

			await CreateHandlerAndAddToWindow(page, () =>
			{
				attachmentCallbackObserved = true;

				var wideHandler = wideRotatedLabel.Handler as LabelHandler;
				var narrowHandler = narrowRotatedLabel.Handler as LabelHandler;
				var mediumHandler = mediumRotatedLabel.Handler as LabelHandler;
				var wideUnrotatedHandler = wideLabel.Handler as LabelHandler;
				var wideContainerHandler = wideRotatedContainer.Handler as LayoutHandler;
				var narrowContainerHandler = narrowRotatedContainer.Handler as LayoutHandler;
				var mediumContainerHandler = mediumRotatedContainer.Handler as LayoutHandler;

				handlersObserved =
					wideHandler is not null &&
					narrowHandler is not null &&
					mediumHandler is not null &&
					wideUnrotatedHandler is not null &&
					wideContainerHandler is not null &&
					narrowContainerHandler is not null &&
					mediumContainerHandler is not null;

				if (handlersObserved)
				{
					TextBlock wideTextBlock = wideHandler.PlatformView;
					TextBlock narrowTextBlock = narrowHandler.PlatformView;
					TextBlock mediumTextBlock = mediumHandler.PlatformView;
					TextBlock wideUnrotatedTextBlock = wideUnrotatedHandler.PlatformView;

					nativeViewsObserved =
						wideTextBlock is not null &&
						narrowTextBlock is not null &&
						mediumTextBlock is not null &&
						wideUnrotatedTextBlock is not null;

					if (nativeViewsObserved)
					{
						nativeAttachmentObserved =
							wideTextBlock.XamlRoot is not null &&
							narrowTextBlock.XamlRoot is not null &&
							mediumTextBlock.XamlRoot is not null &&
							wideUnrotatedTextBlock.XamlRoot is not null;
						arrangedTextObserved =
							wideTextBlock.Text == labelText &&
							narrowTextBlock.Text == labelText &&
							mediumTextBlock.Text == labelText &&
							wideUnrotatedTextBlock.Text == labelText;
						nonzeroNativeFramesObserved =
							wideTextBlock.ActualWidth > 0 &&
							wideTextBlock.ActualHeight > 0 &&
							narrowTextBlock.ActualWidth > 0 &&
							narrowTextBlock.ActualHeight > 0 &&
							mediumTextBlock.ActualWidth > 0 &&
							mediumTextBlock.ActualHeight > 0 &&
							wideUnrotatedTextBlock.ActualWidth > 0 &&
							wideUnrotatedTextBlock.ActualHeight > 0;

						wideNativeWidth = wideTextBlock.ActualWidth;
						narrowNativeWidth = narrowTextBlock.ActualWidth;
						intrinsicTextWidth = wideUnrotatedTextBlock.ActualWidth;
						topRowHeight = wideContainerHandler.PlatformView.ActualHeight;
						narrowColumnWidth = narrowContainerHandler.PlatformView.ActualWidth;
						mediumColumnWidth = mediumContainerHandler.PlatformView.ActualWidth;

					}
				}
			});

			Assert.True(attachmentCallbackObserved, "The post-attachment layout callback did not run.");
			Assert.True(handlersObserved, "The expected Page, Grid, and Label handler hierarchy was not created.");
			Assert.True(nativeViewsObserved, "The expected WinUI TextBlocks were not created.");
			Assert.True(nativeAttachmentObserved, "The rotated WinUI TextBlocks were not attached to a XamlRoot.");
			Assert.True(arrangedTextObserved, "The rotated WinUI TextBlocks did not contain the arranged text.");
			Assert.True(nonzeroNativeFramesObserved, "The rotated WinUI TextBlocks did not receive nonzero native frames.");
			Assert.True(Math.Abs(narrowColumnWidth - 40) <= tolerance,
				$"The narrow Grid column was not arranged to 40 units. Actual: {narrowColumnWidth:F2}.");
			Assert.True(Math.Abs(mediumColumnWidth - 80) <= tolerance,
				$"The medium Grid column was not arranged to 80 units. Actual: {mediumColumnWidth:F2}.");
			Assert.True(intrinsicTextWidth > 0, "The attached wide TextBlock did not produce an intrinsic text width.");
			Assert.True(topRowHeight + tolerance >= intrinsicTextWidth,
				$"The top row height {topRowHeight:F2} could not contain the intrinsic text width {intrinsicTextWidth:F2}.");
			Assert.True(Math.Abs(wideNativeWidth - intrinsicTextWidth) <= tolerance,
				$"The wide rotated Label was not an unclipped intrinsic-width oracle. Actual: {wideNativeWidth:F2}, intrinsic: {intrinsicTextWidth:F2}.");
			Assert.True(narrowNativeWidth + tolerance >= intrinsicTextWidth,
				$"Rotated Label native width did not preserve its intrinsic text width. Actual: {narrowNativeWidth:F2}, intrinsic: {intrinsicTextWidth:F2}.");
		}
	}
}
#endif

