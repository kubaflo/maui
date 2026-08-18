using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Entry)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37151 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task SemanticDescriptionIsExposedAsEntryContentDescription()
		{
			const string entryDescription = "SemanticDecription_Entry";
			const string labelDescription = "SemanticDescription_Label";

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var entry = new Entry
			{
				AutomationId = "AutomationID_Entry",
				Placeholder = "Enter text here",
			};
			SemanticProperties.SetDescription(entry, entryDescription);

			var label = new Label
			{
				AutomationId = "AutomationID_Label",
				Text = "Name",
				VerticalOptions = LayoutOptions.Start,
			};
			SemanticProperties.SetDescription(label, labelDescription);

			var grid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
				},
			};
			grid.Add(entry, 0, 0);
			grid.Add(label, 0, 1);

			var page = new ContentPage { Content = grid };
			int pageLoaded = 0;
			int entryLoaded = 0;
			int labelLoaded = 0;
			page.Loaded += (_, _) => pageLoaded++;
			entry.Loaded += (_, _) => entryLoaded++;
			label.Loaded += (_, _) => labelLoaded++;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await OnLoadedAsync(page);
				await OnLoadedAsync(entry);
				await OnLoadedAsync(label);

				Assert.True(pageLoaded > 0, "The ContentPage did not load.");
				Assert.True(entryLoaded > 0, "The Entry did not load.");
				Assert.True(labelLoaded > 0, "The Label did not load.");

				var entryHandler = Assert.IsType<EntryHandler>(entry.Handler);
				var labelHandler = Assert.IsType<LabelHandler>(label.Handler);
				Assert.NotNull(entryHandler.PlatformView);
				Assert.NotNull(labelHandler.PlatformView);

				bool captureOccurred = false;
				string entryContentDescription = "<not-observed>";
				string entryText = "<not-observed>";
				string labelContentDescription = "<not-observed>";

				await AssertEventually(() =>
				{
					using var entryNode = entryHandler.PlatformView.CreateAccessibilityNodeInfo();
					using var labelNode = labelHandler.PlatformView.CreateAccessibilityNodeInfo();
					if (entryNode is null || labelNode is null)
						return false;

					entryContentDescription = entryNode.ContentDescription?.ToString();
					entryText = entryNode.Text?.ToString();
					labelContentDescription = labelNode.ContentDescription?.ToString();
					captureOccurred = true;
					return true;
				}, message: "Android accessibility nodes were not available.");

				Assert.True(captureOccurred, "Android accessibility semantics were not captured.");
				Assert.NotEqual("<not-observed>", entryContentDescription);
				Assert.NotEqual("<not-observed>", entryText);
				Assert.NotEqual("<not-observed>", labelContentDescription);

				Assert.True(
					labelContentDescription == labelDescription,
					$"Label accessibility node contentDescription was '{FormatObserved(labelContentDescription)}'; expected '{labelDescription}'.");
				Assert.True(
					entryContentDescription == entryDescription,
					$"Entry accessibility node contentDescription was '{FormatObserved(entryContentDescription)}'; expected '{entryDescription}'.");
				Assert.True(
					entryText != entryDescription,
					$"Entry accessibility node text was incorrectly replaced with '{entryDescription}'.");
			});
		}

		static string FormatObserved(string value) =>
			string.IsNullOrEmpty(value) ? "<empty>" : value;
	}
}
