#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue28502")]
	public class Issue28502 : ControlsHandlerTestBase
	{
		const string MissingFontFamily = "OpenSansRegular";
		const string MissingTtfCandidate = "Assets/Fonts/OpenSansRegular.ttf";
		const string MissingOtfCandidate = "Assets/Fonts/OpenSansRegular.otf";

		[Fact]
		public async Task UnregisteredFontFamilyDoesNotProbeMissingAppAssets()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var affectedLabel = new Label
			{
				Text = "Affected label remains visible",
				FontSize = 22
			};

			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Missing OpenSansRegular font",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "The affected text starts with the platform default font. Apply the removed, unregistered family to reproduce the Windows font lookup.",
						FontSize = 16
					},
					affectedLabel,
					new Label
					{
						Text = "Native font source: platform default",
						FontSize = 14
					},
					new Button
					{
						Text = "Apply removed OpenSansRegular font"
					}
				}
			};

			var page = new ContentPage
			{
				Title = "Missing font probe",
				Content = new ScrollView { Content = content }
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(affectedLabel.Handler);
				var textBlock = Assert.IsType<WTextBlock>(labelHandler.PlatformView);
				Assert.Equal(affectedLabel.Text, textBlock.Text);

				var initialSource = textBlock.FontFamily.Source;
				Assert.True(
					!ContainsMissingAssetCandidate(initialSource),
					$"Observed initial native font source '{initialSource}'. Expected neither '{MissingTtfCandidate}' nor '{MissingOtfCandidate}'.");

				var callbackObserved = false;
				var observedSource = "<font family callback not observed>";
				var callbackToken = textBlock.RegisterPropertyChangedCallback(
					WTextBlock.FontFamilyProperty,
					(_, _) =>
					{
						callbackObserved = true;
						observedSource = textBlock.FontFamily.Source;
					});

				try
				{
					affectedLabel.FontFamily = MissingFontFamily;

					await AssertEventually(
						() => callbackObserved,
						timeout: 5000,
						message: $"Observed native font source '{observedSource}'. Expected a FontFamily callback after setting '{MissingFontFamily}'.");

					Assert.True(
						callbackObserved,
						$"Observed native font source '{observedSource}'. Expected a FontFamily callback after setting '{MissingFontFamily}'.");
					Assert.True(
						observedSource.Contains(MissingFontFamily, StringComparison.Ordinal),
						$"Observed native font source '{observedSource}'. Expected it to retain system-family candidate '{MissingFontFamily}'.");
					Assert.True(
						!ContainsMissingAssetCandidate(observedSource),
						$"Unregistered OpenSansRegular should not probe missing app font assets. Observed native font source '{observedSource}'. Expected neither '{MissingTtfCandidate}' nor '{MissingOtfCandidate}'.");
				}
				finally
				{
					textBlock.UnregisterPropertyChangedCallback(WTextBlock.FontFamilyProperty, callbackToken);
				}
			});
		}

		static bool ContainsMissingAssetCandidate(string source) =>
			source.Contains(MissingTtfCandidate, StringComparison.Ordinal) ||
			source.Contains(MissingOtfCandidate, StringComparison.Ordinal);
	}
}
#endif

