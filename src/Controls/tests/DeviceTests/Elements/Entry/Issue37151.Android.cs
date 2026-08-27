#if ANDROID
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37151")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37151 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EntrySemanticDescriptionMapsToContentDescription()
		{
			const string notCaptured = "__NOT_CAPTURED__";
			const string expectedEntryDescription = "SemanticDecription_Entry";
			const string expectedLabelDescription = "SemanticDescription_Label";

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

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

			var grid = new Grid
			{
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star)
				}
			};
			grid.Add(entry);
			Grid.SetRow(entry, 0);
			grid.Add(label);
			Grid.SetRow(label, 1);

			var page = new ContentPage { Content = grid };
			var attachmentCount = 0;
			var entryContentDescription = notCaptured;
			var entryAccessibilityText = notCaptured;
			var labelContentDescription = notCaptured;
			var entryDescriptionIsExpected = false;
			var entryTextShowsDescription = false;
			var labelDescriptionIsExpected = false;

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(page, async windowHandler =>
			{
				attachmentCount++;
				Assert.Equal(1, attachmentCount);

				Assert.NotNull(page.Handler);
				var entryHandler = Assert.IsType<EntryHandler>(entry.Handler);
				var labelHandler = Assert.IsType<LabelHandler>(label.Handler);
				var entryView = Assert.IsAssignableFrom<AppCompatEditText>(entryHandler.PlatformView);
				var labelView = Assert.IsAssignableFrom<AppCompatTextView>(labelHandler.PlatformView);
				Assert.NotNull(windowHandler.PlatformViewUnderTest);
				Assert.True(entryView.IsAttachedToWindow, "Entry platform view was not attached to a window.");
				Assert.True(labelView.IsAttachedToWindow, "Label platform view was not attached to a window.");
				Assert.Same(windowHandler.PlatformViewUnderTest.RootView, entryView.RootView);
				Assert.Same(windowHandler.PlatformViewUnderTest.RootView, labelView.RootView);

				Assert.Equal(expectedEntryDescription, SemanticProperties.GetDescription(entry));
				Assert.Equal(expectedLabelDescription, SemanticProperties.GetDescription(label));
				Assert.Equal(entry.Placeholder, entryView.Hint);
				Assert.Equal(label.Text, labelView.Text);

				await AssertEventually(() =>
				{
					using var entryNode = entryView.CreateAccessibilityNodeInfo();
					using var labelNode = labelView.CreateAccessibilityNodeInfo();
					Assert.NotNull(entryNode);
					Assert.NotNull(labelNode);

					entryContentDescription = entryNode.ContentDescription?.ToString() ?? string.Empty;
					entryAccessibilityText = entryNode.Text?.ToString() ?? string.Empty;
					labelContentDescription = labelNode.ContentDescription?.ToString() ?? string.Empty;
					entryDescriptionIsExpected = entryContentDescription == expectedEntryDescription;
					entryTextShowsDescription = entryAccessibilityText == expectedEntryDescription;
					labelDescriptionIsExpected = labelContentDescription == expectedLabelDescription;

					return labelDescriptionIsExpected && (entryDescriptionIsExpected || entryTextShowsDescription);
				}, message: "Accessibility nodes did not settle to the expected Entry or reported defect state.");
			});

			Assert.Equal(1, attachmentCount);
			Assert.NotEqual(notCaptured, entryContentDescription);
			Assert.NotEqual(notCaptured, entryAccessibilityText);
			Assert.NotEqual(notCaptured, labelContentDescription);
			Assert.True(labelDescriptionIsExpected,
				$"Label contentDescription was '{labelContentDescription}', expected '{expectedLabelDescription}'.");
			Assert.True(entryDescriptionIsExpected,
				$"Issue37151: Android Entry contentDescription was '{entryContentDescription}', expected '{expectedEntryDescription}'; accessibility text was '{entryAccessibilityText}'.");
		}
	}
}
#endif

