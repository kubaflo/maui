#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Accessibility)]
	[Category("Issue34380")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue34380 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task GestureLayoutsExposeActionAndChildSemanticsToVoiceOver()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var activationIndex = 0;
			var noHintActivationOrder = -1;
			var hintedActivationOrder = -1;

			var noHintTitle = new Label { Text = "Layout without semantic hint" };
			var noHintDescription = new Label { Text = "VoiceOver should identify this layout as interactable." };
			var noHintTap = new TapGestureRecognizer();
			noHintTap.Tapped += (sender, args) => noHintActivationOrder = activationIndex++;

			var noHintLayout = new VerticalStackLayout
			{
				AutomationId = "NoHintGestureLayout",
				Spacing = 4
			};
			noHintLayout.GestureRecognizers.Add(noHintTap);
			noHintLayout.Add(noHintTitle);
			noHintLayout.Add(noHintDescription);

			var hintedTitle = new Label { Text = "Layout with semantic hint" };
			var hintedFirstChild = new Label { Text = "First child label must be announced." };
			var hintedSecondChild = new Label { Text = "Second child label must also be announced." };
			var hintedTap = new TapGestureRecognizer();
			hintedTap.Tapped += (sender, args) => hintedActivationOrder = activationIndex++;

			var hintedLayout = new VerticalStackLayout
			{
				AutomationId = "HintGestureLayout",
				Spacing = 4
			};
			SemanticProperties.SetHint(hintedLayout, "Activates the hinted layout");
			hintedLayout.GestureRecognizers.Add(hintedTap);
			hintedLayout.Add(hintedTitle);
			hintedLayout.Add(hintedFirstChild);
			hintedLayout.Add(hintedSecondChild);

			var heading = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 20,
				Text = "VoiceOver gesture semantics"
			};
			var instructions = new Label { Text = "With VoiceOver enabled, focus and activate each layout in order." };
			var rootLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 24
			};
			rootLayout.Add(heading);
			rootLayout.Add(instructions);
			rootLayout.Add(noHintLayout);
			rootLayout.Add(hintedLayout);

			var page = new ContentPage
			{
				Title = "VoiceOver gesture semantics",
				Content = new ScrollView { Content = rootLayout }
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async handler =>
			{
				Assert.NotNull(handler);
				Assert.Equal("NoHintGestureLayout", noHintLayout.AutomationId);
				Assert.Equal("HintGestureLayout", hintedLayout.AutomationId);
				Assert.Single(noHintLayout.GestureRecognizers);
				Assert.Single(hintedLayout.GestureRecognizers);
				Assert.Equal("VoiceOver gesture semantics", heading.Text);
				Assert.Equal("With VoiceOver enabled, focus and activate each layout in order.", instructions.Text);
				Assert.Equal("Layout without semantic hint", noHintTitle.Text);
				Assert.Equal("VoiceOver should identify this layout as interactable.", noHintDescription.Text);
				Assert.Equal("Layout with semantic hint", hintedTitle.Text);
				Assert.Equal("First child label must be announced.", hintedFirstChild.Text);
				Assert.Equal("Second child label must also be announced.", hintedSecondChild.Text);
				Assert.Null(SemanticProperties.GetHint(noHintLayout));
				Assert.Equal("Activates the hinted layout", SemanticProperties.GetHint(hintedLayout));
				Assert.Same(noHintLayout, rootLayout.Children[2]);
				Assert.Same(hintedLayout, rootLayout.Children[3]);

				Assert.NotNull(noHintLayout.Handler);
				Assert.NotNull(hintedLayout.Handler);
				var noHintView = Assert.IsAssignableFrom<UIView>(noHintLayout.Handler.PlatformView);
				var hintedView = Assert.IsAssignableFrom<UIView>(hintedLayout.Handler.PlatformView);
				Assert.NotNull(noHintView.Window);
				Assert.NotNull(hintedView.Window);

				var noHintHandled = noHintView.AccessibilityActivate();
				await AssertEventually(
					() => noHintActivationOrder == 0,
					message: $"Issue34380 native VoiceOver semantics mismatch: no-hint activation order was {noHintActivationOrder}; expected 0.");

				var hintedHandled = hintedView.AccessibilityActivate();
				await AssertEventually(
					() => hintedActivationOrder == 1,
					message: $"Issue34380 native VoiceOver semantics mismatch: hinted activation order was {hintedActivationOrder}; expected 1.");

				Assert.True(
					noHintView.IsAccessibilityElement,
					$"Issue34380 native VoiceOver semantics mismatch: no-hint IsAccessibilityElement was {noHintView.IsAccessibilityElement}; expected True so VoiceOver can focus the actionable layout.");
				Assert.True(
					noHintView.ShouldGroupAccessibilityChildren,
					$"Issue34380 native VoiceOver semantics mismatch: no-hint ShouldGroupAccessibilityChildren was {noHintView.ShouldGroupAccessibilityChildren}; expected True.");
				Assert.True(
					noHintHandled,
					$"Issue34380 native VoiceOver semantics mismatch: no-hint AccessibilityActivate returned {noHintHandled}; expected True.");
				Assert.True(
					hintedView.IsAccessibilityElement,
					$"Issue34380 native VoiceOver semantics mismatch: hinted IsAccessibilityElement was {hintedView.IsAccessibilityElement}; expected True.");
				Assert.True(
					hintedHandled,
					$"Issue34380 native VoiceOver semantics mismatch: hinted AccessibilityActivate returned {hintedHandled}; expected True.");
				Assert.True(
					string.Equals("Activates the hinted layout", hintedView.AccessibilityHint, StringComparison.Ordinal),
					$"Issue34380 native VoiceOver semantics mismatch: hinted AccessibilityHint was '{hintedView.AccessibilityHint}'; expected 'Activates the hinted layout'.");
				Assert.True(
					hintedView.AccessibilityLabel?.Contains("Layout with semantic hint", StringComparison.Ordinal) == true,
					$"Issue34380 native VoiceOver semantics mismatch: hinted AccessibilityLabel was '{hintedView.AccessibilityLabel}'; expected it to contain 'Layout with semantic hint'.");
				Assert.True(
					hintedView.AccessibilityLabel?.Contains("First child label must be announced.", StringComparison.Ordinal) == true,
					$"Issue34380 native VoiceOver semantics mismatch: hinted AccessibilityLabel was '{hintedView.AccessibilityLabel}'; expected it to contain 'First child label must be announced.'.");
				Assert.True(
					hintedView.AccessibilityLabel?.Contains("Second child label must also be announced.", StringComparison.Ordinal) == true,
					$"Issue34380 native VoiceOver semantics mismatch: hinted AccessibilityLabel was '{hintedView.AccessibilityLabel}'; expected it to contain 'Second child label must also be announced.'.");
			});
		}
	}
}
#endif

