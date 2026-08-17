using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
#if !MACCATALYST
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.Button)]
	public class Issue36697 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RuntimeTextColorUpdatesAttributedTitleWithCharacterSpacing()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var referenceButton = new Button
			{
				Text = "Reference Red Text",
				CharacterSpacing = 5,
				TextColor = Colors.Red,
			};
			var defaultReferenceButton = new Button
			{
				Text = "Reference Default Text",
			};
			var affectedButton = new Button
			{
				Text = "Affected Button Text",
				CharacterSpacing = 5,
			};
			var setRedButton = new Button
			{
				Text = "Set TextColor Red",
			};
			var resetColorButton = new Button
			{
				Text = "Reset TextColor Default",
			};
			var resultLabel = new Label
			{
				Text = "NO BUG:",
				FontAttributes = FontAttributes.Bold,
			};
			var clickCount = -1;

			setRedButton.Clicked += (sender, args) =>
			{
				affectedButton.TextColor = Colors.Red;
				clickCount++;
			};
			resetColorButton.Clicked += (sender, args) =>
			{
				affectedButton.TextColor = null;
				clickCount++;
			};

			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 14,
				Children =
				{
					new Label
					{
						Text = "Issue 36697: runtime Button TextColor with CharacterSpacing",
						FontAttributes = FontAttributes.Bold,
						FontSize = 18,
					},
					new Label
					{
						Text = "The affected button starts with the platform-default text color. After Set TextColor Red, its text should match the red reference button.",
					},
					new Label { Text = "Expected red reference (configured before display):" },
					referenceButton,
					new Label { Text = "Platform-default reference:" },
					defaultReferenceButton,
					new Label { Text = "Affected runtime-updated button:" },
					affectedButton,
					setRedButton,
					resetColorButton,
					resultLabel,
				},
			};
			var page = new ContentPage
			{
				Content = new ScrollView { Content = stack },
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var referenceNative = Assert.IsType<UIButton>(Assert.IsType<ButtonHandler>(referenceButton.Handler).PlatformView);
				var defaultReferenceNative = Assert.IsType<UIButton>(Assert.IsType<ButtonHandler>(defaultReferenceButton.Handler).PlatformView);
				var affectedNative = Assert.IsType<UIButton>(Assert.IsType<ButtonHandler>(affectedButton.Handler).PlatformView);
				var setRedNative = Assert.IsType<UIButton>(Assert.IsType<ButtonHandler>(setRedButton.Handler).PlatformView);
				var resetColorNative = Assert.IsType<UIButton>(Assert.IsType<ButtonHandler>(resetColorButton.Handler).PlatformView);

				var defaultColor = GetEffectiveTitleColor(defaultReferenceNative);
				Assert.True(
					ColorComparison.ARGBEquivalent(defaultColor, GetEffectiveTitleColor(affectedNative)),
					"The affected button should initially use the active iOS platform-default title color.");
				Assert.True(
					ColorComparison.ARGBEquivalent(UIColor.Red, GetEffectiveTitleColor(referenceNative)),
					"The configured reference button should have a red native title.");

				clickCount = 0;
				await Trigger(setRedNative, 1);
				var firstRed = GetAttributedTitleColor(affectedNative);
				await Trigger(resetColorNative, 2);
				var firstReset = GetEffectiveTitleColor(affectedNative);
				await Trigger(setRedNative, 3);
				var secondRed = GetAttributedTitleColor(affectedNative);
				await Trigger(resetColorNative, 4);
				var secondReset = GetEffectiveTitleColor(affectedNative);

				Assert.True(
					ColorComparison.ARGBEquivalent(UIColor.Red, firstRed),
					"Runtime TextColor should update the attributed iOS button title to red.");
				Assert.True(
					ColorComparison.ARGBEquivalent(defaultColor, firstReset),
					"Resetting TextColor should restore the active iOS platform-default title color.");
				Assert.True(
					ColorComparison.ARGBEquivalent(UIColor.Red, secondRed),
					"Repeated runtime TextColor updates should keep the attributed iOS button title red.");
				Assert.True(
					ColorComparison.ARGBEquivalent(defaultColor, secondReset),
					"Repeated TextColor resets should restore the active iOS platform-default title color.");

				async Task Trigger(UIButton trigger, int expectedClickCount)
				{
					trigger.SendActionForControlEvents(UIControlEvent.TouchUpInside);
					await AssertHelpers.AssertEventually(
						() => clickCount == expectedClickCount,
						message: $"Expected Clicked callback count {expectedClickCount}.");
				}
			});
		}

		static UIColor GetAttributedTitleColor(UIButton button)
		{
			var attributedTitle = button.CurrentAttributedTitle;
			if (attributedTitle is null)
				return UIColor.Clear;

			return attributedTitle.GetAttribute(UIStringAttributeKey.ForegroundColor, 0, out _) as UIColor
				?? UIColor.Clear;
		}

		static UIColor GetEffectiveTitleColor(UIButton button)
		{
			var attributedTitle = button.CurrentAttributedTitle;
			if (attributedTitle is not null)
			{
				var value = attributedTitle.GetAttribute(UIStringAttributeKey.ForegroundColor, 0, out _);
				if (value is UIColor attributedColor)
					return attributedColor;
			}

			return button.CurrentTitleColor;
		}
	}
#endif
}
