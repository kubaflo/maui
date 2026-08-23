#if IOS && !MACCATALYST
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.ScrollView)]
	[Category("Issue36801")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36801 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ProgrammaticScrollToEndUsesAdjustedContentInset()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
				});
			});

			var scrollToEndButton = new Button { Text = "Scroll to end" };
			var resultLabel = new Label
			{
				Text = "Scroll endpoint",
				FontAttributes = FontAttributes.Bold,
				FontSize = 18
			};
			var captureRangeButton = new Button { Text = "Capture inset range" };
			var rangeStateLabel = new Label { Text = "WAITING FOR INSET RANGE" };
			var programmaticStateLabel = new Label { Text = "WAITING FOR PROGRAMMATIC SCROLL" };
			var armFingerScrollButton = new Button { Text = "Arm finger scroll" };
			var fingerStateLabel = new Label { Text = "FINGER SCROLL NOT ARMED" };
			var checkResultButton = new Button { Text = "Check result" };
			var probeLabel = new Label
			{
				Text = "BOTTOM PROBE",
				FontSize = 18,
				FontAttributes = FontAttributes.Bold
			};

			var stack = new VerticalStackLayout
			{
				Padding = 16,
				Spacing = 6,
				Children =
				{
					scrollToEndButton,
					resultLabel,
					captureRangeButton,
					rangeStateLabel
				}
			};

			var fillerLabels = new List<Label>();
			for (var index = 0; index < 40; index++)
			{
				var fillerLabel = new Label { Text = $"Filler row {index + 1}" };
				fillerLabels.Add(fillerLabel);
				stack.Children.Add(fillerLabel);
			}

			stack.Children.Add(programmaticStateLabel);
			stack.Children.Add(armFingerScrollButton);
			stack.Children.Add(fingerStateLabel);
			stack.Children.Add(checkResultButton);
			stack.Children.Add(probeLabel);

			var scrollView = new ScrollView { Content = stack };
			var page = new ContentPage
			{
				SafeAreaEdges = SafeAreaEdges.None,
				Content = scrollView
			};
			var testWindow = new Window(page);

			await CreateHandlerAndAddToWindow(testWindow, async () =>
			{
				Assert.Same(stack, scrollView.Content);
				Assert.Equal(49, stack.Children.Count);
				Assert.Same(fillerLabels[0], stack.Children[4]);
				Assert.Same(fillerLabels[39], stack.Children[43]);
				Assert.Same(probeLabel, stack.Children[stack.Children.Count - 1]);

				var nativeScrollView = Assert.IsType<MauiScrollView>(scrollView.Handler.PlatformView);
				await AssertEventually(
					() => nativeScrollView.Bounds.Height > 0 &&
						nativeScrollView.ContentSize.Height > nativeScrollView.Bounds.Height &&
						scrollView.ContentSize.Height > 0,
					timeout: 5000,
					message: "ScrollView content and viewport did not finish layout");

				Assert.Equal(UIScrollViewContentInsetAdjustmentBehavior.Automatic, nativeScrollView.ContentInsetAdjustmentBehavior);
				Assert.True(
					nativeScrollView.AdjustedContentInset.Bottom > 0.5,
					$"Expected a nonzero adjusted bottom safe-area inset, but observed {nativeScrollView.AdjustedContentInset.Bottom:0.##}.");

				var adjustedInsets = nativeScrollView.AdjustedContentInset;
				var expectedMaximumOffset =
					nativeScrollView.ContentSize.Height +
					adjustedInsets.Bottom -
					nativeScrollView.Bounds.Height;

				var resetOffset = nativeScrollView.ContentOffset.Y;
				var observedScrollOffset = double.NaN;
				void OnScrolled(object sender, ScrolledEventArgs args) => observedScrollOffset = args.ScrollY;

				stack.Children.Remove(resultLabel);
				stack.Children.Insert(stack.Children.IndexOf(probeLabel), resultLabel);
				Assert.Same(resultLabel, stack.Children[stack.Children.Count - 2]);
				Assert.Same(probeLabel, stack.Children[stack.Children.Count - 1]);

				scrollView.Scrolled += OnScrolled;
				await scrollView.ScrollToAsync(0, scrollView.ContentSize.Height, animated: false);
				await AssertEventually(
					() => !double.IsNaN(observedScrollOffset),
					message: "The MAUI Scrolled callback was not raised after ScrollToAsync");
				scrollView.Scrolled -= OnScrolled;

				Assert.NotEqual(resetOffset, nativeScrollView.ContentOffset.Y);

				var actualOffset = nativeScrollView.ContentOffset.Y;
				Assert.True(
					Math.Abs(actualOffset - expectedMaximumOffset) <= 0.5,
					$"Programmatic scroll endpoint did not reach the inset-aware maximum. " +
					$"Observed={actualOffset:0.##}, Expected={expectedMaximumOffset:0.##}, " +
					$"ContentHeight={nativeScrollView.ContentSize.Height:0.##}, " +
					$"BoundsHeight={nativeScrollView.Bounds.Height:0.##}, " +
					$"AdjustedInsets={{Top={adjustedInsets.Top:0.##}, Left={adjustedInsets.Left:0.##}, " +
					$"Bottom={adjustedInsets.Bottom:0.##}, Right={adjustedInsets.Right:0.##}}}");
			});
		}
	}
}
#endif

