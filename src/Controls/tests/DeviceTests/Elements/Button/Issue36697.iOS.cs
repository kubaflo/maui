using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if !MACCATALYST
	[Category(TestCategory.Button)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36697 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CharacterSpacingButtonUpdatesAttributedTitleColor()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
				});
			});

			var affectedButton = new Button
			{
				CharacterSpacing = 5,
				Text = "Affected Button",
			};
			var redReferenceButton = new Button
			{
				CharacterSpacing = 5,
				Text = "Red Reference",
				TextColor = Colors.Red,
			};
			var defaultReferenceButton = new Button
			{
				CharacterSpacing = 5,
				Text = "Default Reference",
			};
			var setRedButton = new Button { Text = "Set TextColor Red" };
			var resetDefaultButton = new Button { Text = "Reset TextColor Default" };
			var transitionLabel = new Label { Text = "Ready: affected button starts at platform default" };
			var resultLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = "NO BUG:",
			};
			var transition = -1;

			setRedButton.Clicked += (_, _) =>
			{
				affectedButton.TextColor = Colors.Red;
				transition = 1;
			};
			resetDefaultButton.Clicked += (_, _) =>
			{
				affectedButton.TextColor = null;
				transition = 2;
			};

			var redReferenceLayout = new VerticalStackLayout
			{
				Spacing = 4,
				Children =
				{
					new Label { Text = "Expected red" },
					redReferenceButton,
				},
			};
			var defaultReferenceLayout = new VerticalStackLayout
			{
				Spacing = 4,
				Children =
				{
					new Label { Text = "Expected default" },
					defaultReferenceButton,
				},
			};
			var referenceGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star),
				},
				ColumnSpacing = 12,
			};
			referenceGrid.Add(redReferenceLayout);
			referenceGrid.Add(defaultReferenceLayout, 1);

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "Button TextColor with CharacterSpacing",
					},
					new Label { Text = "Affected button (CharacterSpacing=5)" },
					affectedButton,
					referenceGrid,
					setRedButton,
					resetDefaultButton,
					transitionLabel,
					resultLabel,
				},
			};
			var page = new ContentPage { Content = layout };

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				Assert.Same(affectedButton, layout.Children[2]);
				Assert.Same(redReferenceButton, redReferenceLayout.Children[1]);
				Assert.Same(defaultReferenceButton, defaultReferenceLayout.Children[1]);
				Assert.Equal(5, affectedButton.CharacterSpacing);
				Assert.Equal(5, redReferenceButton.CharacterSpacing);
				Assert.Equal(5, defaultReferenceButton.CharacterSpacing);

				var affectedHandler = Assert.IsType<ButtonHandler>(affectedButton.Handler);
				var redReferenceHandler = Assert.IsType<ButtonHandler>(redReferenceButton.Handler);
				var defaultReferenceHandler = Assert.IsType<ButtonHandler>(defaultReferenceButton.Handler);
				var setRedHandler = Assert.IsType<ButtonHandler>(setRedButton.Handler);
				var resetDefaultHandler = Assert.IsType<ButtonHandler>(resetDefaultButton.Handler);
				var affectedNative = Assert.IsType<UIButton>(affectedHandler.PlatformView);
				var redReferenceNative = Assert.IsType<UIButton>(redReferenceHandler.PlatformView);
				var defaultReferenceNative = Assert.IsType<UIButton>(defaultReferenceHandler.PlatformView);

				var affectedAttributedTitle = Assert.IsAssignableFrom<NSAttributedString>(
					affectedNative.TitleLabel.AttributedText);
				var redReferenceAttributedTitle = Assert.IsAssignableFrom<NSAttributedString>(
					redReferenceNative.TitleLabel.AttributedText);
				var defaultReferenceAttributedTitle = Assert.IsAssignableFrom<NSAttributedString>(
					defaultReferenceNative.TitleLabel.AttributedText);
				Assert.Equal("Affected Button", affectedAttributedTitle.Value);
				Assert.Equal("Red Reference", redReferenceAttributedTitle.Value);
				Assert.Equal("Default Reference", defaultReferenceAttributedTitle.Value);
				Assert.NotNull(affectedNative.Window);
				Assert.Same(affectedNative.Window, redReferenceNative.Window);
				Assert.Same(affectedNative.Window, defaultReferenceNative.Window);
				Assert.Same(affectedNative.Window, setRedHandler.PlatformView.Window);
				Assert.Same(affectedNative.Window, resetDefaultHandler.PlatformView.Window);
				transition = 0;
				setRedHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await AssertEventually(
					() => transition == 1 && affectedButton.TextColor == Colors.Red,
					message: "Issue36697: red action did not update the attached affected Button.TextColor.");
				await AssertEventually(
					() => GetEffectiveTitleColor(affectedNative) == UIColor.Red.ToColor(),
					message: "Issue36697: affected button title label did not render red after TextColor changed to Colors.Red.");

				Assert.Equal(
					GetEffectiveTitleColor(redReferenceNative),
					GetEffectiveTitleColor(affectedNative));

				resetDefaultHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await AssertEventually(
					() => transition == 2 && affectedButton.TextColor is null,
					message: "Issue36697: reset action did not clear the attached affected Button.TextColor.");
				await AssertEventually(
					() => GetEffectiveTitleColor(affectedNative) == GetEffectiveTitleColor(defaultReferenceNative));

				Assert.Equal(defaultReferenceNative.CurrentTitleColor.ToColor(), affectedNative.CurrentTitleColor.ToColor());
			});
		}

		static Color GetEffectiveTitleColor(UIButton button)
		{
			var attributedTitle = button.TitleLabel.AttributedText;
			if (attributedTitle?.GetAttribute(UIStringAttributeKey.ForegroundColor, 0, out _) is UIColor color)
				return color.ToColor();

			return button.CurrentTitleColor.ToColor();
		}
	}
#endif
}
