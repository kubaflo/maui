using System.Threading.Tasks;
using Android.Graphics;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using RadioButton = Microsoft.Maui.Controls.RadioButton;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29674")]
	public class Issue29674 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RuntimeHtmlLabelUpdatesTextDecorations()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<RadioButton, RadioButtonHandler>();
				});
			});

			var headingLabel = new Label
			{
				Text = "HTML TextDecorations runtime test",
				FontAttributes = FontAttributes.Bold
			};
			var runtimeDescriptionLabel = new Label { Text = "Runtime label" };
			var affectedLabel = new Label
			{
				Text = "DECORATION SAMPLE",
				TextType = TextType.Text
			};
			var referenceDescriptionLabel = new Label { Text = "Expected HTML strikethrough reference" };
			var referenceLabel = new Label
			{
				Text = "DECORATION SAMPLE",
				TextType = TextType.Html,
				TextDecorations = TextDecorations.Strikethrough
			};
			var textTypeLabel = new Label { Text = "Text type" };
			var plainRadio = new RadioButton
			{
				Content = "Plain",
				GroupName = "TextType",
				IsChecked = true
			};
			var htmlRadio = new RadioButton
			{
				Content = "HTML",
				GroupName = "TextType"
			};
			var textTypeRow = new HorizontalStackLayout
			{
				Spacing = 18,
				Children =
				{
					plainRadio,
					htmlRadio
				}
			};
			var decorationLabel = new Label { Text = "Text decoration" };
			var noneRadio = new RadioButton
			{
				Content = "None",
				GroupName = "Decoration",
				IsChecked = true
			};
			var underlineRadio = new RadioButton
			{
				Content = "Underline",
				GroupName = "Decoration"
			};
			var strikethroughRadio = new RadioButton
			{
				Content = "Strikethrough",
				GroupName = "Decoration"
			};
			var decorationRow = new HorizontalStackLayout
			{
				Spacing = 18,
				Children =
				{
					noneRadio,
					underlineRadio,
					strikethroughRadio
				}
			};
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 14,
				Children =
				{
					headingLabel,
					runtimeDescriptionLabel,
					affectedLabel,
					referenceDescriptionLabel,
					referenceLabel,
					textTypeLabel,
					textTypeRow,
					decorationLabel,
					decorationRow
				}
			};
			var page = new ContentPage { Content = layout };
			var observedSequence = -1;

			plainRadio.CheckedChanged += OnTextTypeChanged;
			htmlRadio.CheckedChanged += OnTextTypeChanged;
			noneRadio.CheckedChanged += OnDecorationChanged;
			underlineRadio.CheckedChanged += OnDecorationChanged;
			strikethroughRadio.CheckedChanged += OnDecorationChanged;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				VisualElement[] elements =
				[
					page,
					layout,
					headingLabel,
					runtimeDescriptionLabel,
					affectedLabel,
					referenceDescriptionLabel,
					referenceLabel,
					textTypeLabel,
					textTypeRow,
					plainRadio,
					htmlRadio,
					decorationLabel,
					decorationRow,
					noneRadio,
					underlineRadio,
					strikethroughRadio
				];

				foreach (var element in elements)
				{
					Assert.NotNull(element.Handler);
					Assert.NotNull(element.Handler.PlatformView);
				}

				var affectedNativeLabel = affectedLabel.Handler.PlatformView as TextView;
				var referenceNativeLabel = referenceLabel.Handler.PlatformView as TextView;
				var nativeUnderlineRadio = underlineRadio.Handler.PlatformView as AppCompatRadioButton;
				var nativeHtmlRadio = htmlRadio.Handler.PlatformView as AppCompatRadioButton;
				var nativeStrikethroughRadio = strikethroughRadio.Handler.PlatformView as AppCompatRadioButton;

				Assert.NotNull(affectedNativeLabel);
				Assert.NotNull(referenceNativeLabel);
				Assert.NotNull(nativeUnderlineRadio);
				Assert.NotNull(nativeHtmlRadio);
				Assert.NotNull(nativeStrikethroughRadio);
				Assert.Equal("DECORATION SAMPLE", affectedNativeLabel.Text);
				Assert.Equal("DECORATION SAMPLE", referenceNativeLabel.Text);
				Assert.True(referenceNativeLabel.PaintFlags.HasFlag(PaintFlags.StrikeThruText));
				Assert.False(referenceNativeLabel.PaintFlags.HasFlag(PaintFlags.UnderlineText));
				Assert.False(affectedNativeLabel.PaintFlags.HasFlag(PaintFlags.StrikeThruText));
				Assert.False(affectedNativeLabel.PaintFlags.HasFlag(PaintFlags.UnderlineText));

				nativeUnderlineRadio.PerformClick();
				await AssertEventually(
					() => observedSequence == 0,
					message: "Underline CheckedChanged callback did not complete.");
				await AssertEventually(
					() => underlineRadio.IsChecked && affectedLabel.TextDecorations == TextDecorations.Underline,
					message: "Underline selection did not update the affected Label.");
				await AssertEventually(
					() => affectedNativeLabel.PaintFlags.HasFlag(PaintFlags.UnderlineText),
					message: "Plain-text Label did not render underline after the click.");

				nativeHtmlRadio.PerformClick();
				await AssertEventually(
					() => observedSequence == 1,
					message: "HTML CheckedChanged callback did not complete.");
				await AssertEventually(
					() => htmlRadio.IsChecked && affectedLabel.TextType == TextType.Html,
					message: "HTML selection did not update the affected Label.");

				nativeStrikethroughRadio.PerformClick();
				await AssertEventually(
					() => observedSequence == 2,
					message: "Strikethrough CheckedChanged callback did not complete.");
				await AssertEventually(
					() => strikethroughRadio.IsChecked && affectedLabel.TextDecorations == TextDecorations.Strikethrough,
					message: "Strikethrough selection did not update the affected Label.");

				using var expectedBitmap = await referenceNativeLabel.ToBitmap(MauiContext);
				using var actualBitmap = await affectedNativeLabel.ToBitmap(MauiContext);
				var renderingsMatch =
					expectedBitmap.Width == actualBitmap.Width &&
					expectedBitmap.Height == actualBitmap.Height &&
					BitmapsMatch(expectedBitmap, actualBitmap);

				Assert.True(
					renderingsMatch,
					$"Runtime HTML Label rendering did not match the preconfigured HTML strikethrough reference after Strikethrough click. Expected bitmap: {expectedBitmap.Width}x{expectedBitmap.Height}; actual bitmap: {actualBitmap.Width}x{actualBitmap.Height}; actual PaintFlags: {affectedNativeLabel.PaintFlags}; managed TextDecorations: {affectedLabel.TextDecorations}.");
			});

			static bool BitmapsMatch(Bitmap expected, Bitmap actual)
			{
				for (var x = 0; x < expected.Width; x++)
				{
					for (var y = 0; y < expected.Height; y++)
					{
						if (expected.GetPixel(x, y) != actual.GetPixel(x, y))
							return false;
					}
				}

				return true;
			}

			void OnTextTypeChanged(object sender, CheckedChangedEventArgs args)
			{
				if (!args.Value)
					return;

				affectedLabel.TextType = ReferenceEquals(sender, htmlRadio) ? TextType.Html : TextType.Text;

				if (ReferenceEquals(sender, htmlRadio))
					observedSequence = observedSequence == 0 ? 1 : -1;
			}

			void OnDecorationChanged(object sender, CheckedChangedEventArgs args)
			{
				if (!args.Value)
					return;

				affectedLabel.TextDecorations =
					ReferenceEquals(sender, underlineRadio) ? TextDecorations.Underline :
					ReferenceEquals(sender, strikethroughRadio) ? TextDecorations.Strikethrough :
					TextDecorations.None;

				if (ReferenceEquals(sender, underlineRadio))
					observedSequence = observedSequence == -1 ? 0 : -1;
				else if (ReferenceEquals(sender, strikethroughRadio))
					observedSequence = observedSequence == 1 ? 2 : -1;
			}
		}
	}
}

