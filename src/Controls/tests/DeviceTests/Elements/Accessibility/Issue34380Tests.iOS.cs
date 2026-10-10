#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue34380")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue34380 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task TappableLayoutIsExposedAsButtonWithChildText()
		{
			const string noHintTitle = "No hint gesture target";
			const string noHintDescription = "VoiceOver should identify this layout as interactable.";
			const string hintTitle = "Hinted gesture target";
			const string hintDescription = "VoiceOver should also announce both child labels.";

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			bool noHintActivated = false;
			bool hintActivated = false;
			bool pageLoaded = false;

			var noHintTap = new TapGestureRecognizer();
			noHintTap.Tapped += (_, _) => noHintActivated = true;

			var noHintLayout = new VerticalStackLayout
			{
				Spacing = 4,
				Children =
				{
					new Label { Text = noHintTitle },
					new Label { Text = noHintDescription },
				},
				GestureRecognizers = { noHintTap },
			};

			var hintTap = new TapGestureRecognizer();
			hintTap.Tapped += (_, _) => hintActivated = true;

			var hintLayout = new VerticalStackLayout
			{
				Spacing = 4,
				Children =
				{
					new Label { Text = hintTitle },
					new Label { Text = hintDescription },
				},
				GestureRecognizers = { hintTap },
			};
			SemanticProperties.SetHint(hintLayout, "Activates the hinted action");

			var rootLayout = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 24,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "VoiceOver gesture semantics",
					},
					noHintLayout,
					hintLayout,
				},
			};

			var page = new ContentPage { Content = rootLayout };
			page.Loaded += (_, _) => pageLoaded = true;

			Assert.False(page.IsLoaded);
			Assert.False(pageLoaded);

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				Assert.True(page.IsLoaded);
				Assert.True(pageLoaded);

				var noHintNative = noHintLayout.Handler?.PlatformView as MauiView;
				var hintNative = hintLayout.Handler?.PlatformView as MauiView;

				Assert.NotNull(noHintNative);
				Assert.NotNull(hintNative);
				Assert.NotNull(noHintNative.Window);
				Assert.NotNull(hintNative.Window);

				await AssertEventually(
					() => hintNative.IsAccessibilityElement
						&& HasButtonTrait(hintNative)
						&& ContainsText(hintNative.AccessibilityLabel, hintTitle)
						&& ContainsText(hintNative.AccessibilityLabel, hintDescription),
					message: "The hinted gesture target did not expose its native accessibility calibration state.");

				bool staticStateObserved = false;
				bool isAccessibilityElement = false;
				UIAccessibilityTrait accessibilityTraits = UIAccessibilityTrait.None;
				string accessibilityLabel = "__not observed__";
				bool shouldGroupChildren = false;
				bool windowAttached = false;

				await AssertEventually(
					() =>
					{
						isAccessibilityElement = noHintNative.IsAccessibilityElement;
						accessibilityTraits = noHintNative.AccessibilityTraits;
						accessibilityLabel = noHintNative.AccessibilityLabel ?? string.Empty;
						shouldGroupChildren = noHintNative.ShouldGroupAccessibilityChildren;
						windowAttached = noHintNative.Window is not null;
						staticStateObserved = true;
						return windowAttached && HasButtonTrait(noHintNative);
					},
					message: "The no-hint gesture target did not reach its attached native accessibility state.");

				Assert.True(staticStateObserved, "The no-hint native accessibility state was not observed.");

				bool noHintActivationHandled = noHintNative.AccessibilityActivate();
				Assert.True(noHintActivationHandled, "The no-hint gesture target did not handle accessibility activation.");
				Assert.True(noHintActivated, "The no-hint gesture target did not raise its TapGestureRecognizer.");

				bool hintActivationHandled = hintNative.AccessibilityActivate();
				Assert.True(hintActivationHandled, "The hinted gesture target did not handle accessibility activation.");
				Assert.True(hintActivated, "The hinted gesture target did not raise its TapGestureRecognizer.");

				bool hasButtonTrait =
					(accessibilityTraits & UIAccessibilityTrait.Button) == UIAccessibilityTrait.Button;
				bool hasFirstChildText = ContainsText(accessibilityLabel, noHintTitle);
				bool hasSecondChildText = ContainsText(accessibilityLabel, noHintDescription);

				Assert.True(
					isAccessibilityElement && hasButtonTrait && hasFirstChildText && hasSecondChildText,
					$"Issue 34380 no-hint gesture target remained unreachable to VoiceOver. " +
					$"IsAccessibilityElement={isAccessibilityElement}; AccessibilityTraits={accessibilityTraits}; " +
					$"AccessibilityLabel='{accessibilityLabel}'; ShouldGroupAccessibilityChildren={shouldGroupChildren}; " +
					$"WindowAttached={windowAttached}; ActivationHandled={noHintActivationHandled}; " +
					$"TapRaised={noHintActivated}; ExpectedIsAccessibilityElement=True; ExpectedButtonTrait=True; " +
					$"ExpectedChildText='{noHintTitle}' and '{noHintDescription}'.");
			});
		}

		static bool HasButtonTrait(MauiView view) =>
			(view.AccessibilityTraits & UIAccessibilityTrait.Button) == UIAccessibilityTrait.Button;

		static bool ContainsText(string value, string expected) =>
			value?.Contains(expected, StringComparison.Ordinal) == true;
	}
}
#endif

