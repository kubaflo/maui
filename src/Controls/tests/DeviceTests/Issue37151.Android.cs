#if ANDROID
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37151")]
	public class Issue37151 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EntrySemanticDescriptionMapsToNativeContentDescription()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			await InvokeOnMainThreadAsync(async () =>
			{
				const string expectedEntryDescription = "SemanticDecription_Entry";
				const string expectedLabelDescription = "SemanticDescription_Label";

				var entry = new Entry
				{
					AutomationId = "AutomationID_Entry",
					Placeholder = "Enter text here"
				};
				SemanticProperties.SetDescription(entry, expectedEntryDescription);

				var label = new Label
				{
					AutomationId = "AutomationID_Label",
					Text = "Name"
				};
				SemanticProperties.SetDescription(label, expectedLabelDescription);
				Grid.SetRow(label, 1);

				var contentGrid = new Grid
				{
					RowDefinitions =
					{
						new RowDefinition(GridLength.Star),
						new RowDefinition(GridLength.Star)
					},
					Children =
					{
						entry,
						label
					}
				};

				var inspectionButton = new Button
				{
					Text = "Inspect content description"
				};
				Grid.SetRow(inspectionButton, 1);

				var resultLabel = new Label
				{
					AutomationId = "SemanticResult",
					Text = "Content description inspection"
				};
				Grid.SetRow(resultLabel, 2);

				var outerGrid = new Grid
				{
					Padding = 24,
					RowSpacing = 16,
					RowDefinitions =
					{
						new RowDefinition(GridLength.Star),
						new RowDefinition(GridLength.Auto),
						new RowDefinition(GridLength.Auto)
					},
					Children =
					{
						contentGrid,
						inspectionButton,
						resultLabel
					}
				};

				var page = new ContentPage
				{
					Content = outerGrid
				};

				var callbackRan = false;
				MauiAppCompatEditText platformEntry = null;
				string entryContentDescription = null;

				await CreateHandlerAndAddToWindow(page, () =>
				{
					callbackRan = true;

					Assert.NotNull(entry.Handler);
					Assert.NotNull(label.Handler);

					platformEntry = Assert.IsType<MauiAppCompatEditText>(entry.Handler.PlatformView);
					Assert.NotNull(label.Handler.PlatformView);
					entryContentDescription = platformEntry.ContentDescription;
				});

				Assert.True(callbackRan);
				Assert.NotNull(platformEntry);
				Assert.Equal(expectedEntryDescription, SemanticProperties.GetDescription(entry));
				Assert.Equal(expectedLabelDescription, SemanticProperties.GetDescription(label));
				Assert.True(
					string.Equals(expectedEntryDescription, entryContentDescription, StringComparison.Ordinal),
					$"Entry native contentDescription did not match SemanticProperties.Description. Expected: '{expectedEntryDescription}', actual: '{entryContentDescription ?? "<null>"}'.");
			});
		}
	}
}
#endif

