#if ANDROID
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
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
		public async Task EntrySemanticDescriptionIsExposedAsContentDescription()
		{
			const string entryDescription = "SemanticDecription_Entry";
			const string labelDescription = "SemanticDescription_Label";

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler<Window, WindowHandlerStub>();
				});
			});

			bool entryLoaded = false;
			var entry = new Entry
			{
				AutomationId = "AutomationID_Entry",
				Placeholder = "Enter text here",
			};
			SemanticProperties.SetDescription(entry, entryDescription);
			entry.Loaded += (_, _) => entryLoaded = true;

			var label = new Label
			{
				AutomationId = "AutomationID_Label",
				Text = "Name",
			};
			SemanticProperties.SetDescription(label, labelDescription);

			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			grid.Add(entry);
			grid.Add(label, row: 1);

			var page = new ContentPage
			{
				Content = grid,
			};

			Assert.False(entryLoaded);
			Assert.Equal("AutomationID_Entry", entry.AutomationId);
			Assert.Equal("Enter text here", entry.Placeholder);
			Assert.Equal(entryDescription, SemanticProperties.GetDescription(entry));
			Assert.Equal("AutomationID_Label", label.AutomationId);
			Assert.Equal("Name", label.Text);
			Assert.Equal(labelDescription, SemanticProperties.GetDescription(label));

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await AssertEventually(
					() => entryLoaded,
					message: "Entry Loaded callback should run after the page is attached.");
				Assert.True(entryLoaded);

				Assert.NotNull(entry.Handler);
				var entryHandler = Assert.IsType<EntryHandler>(entry.Handler);
				Assert.NotNull(entryHandler.PlatformView);
				var entryPlatformView = entryHandler.PlatformView;
				Assert.Same(entryHandler.PlatformView, entryPlatformView);

				Assert.NotNull(label.Handler);
				var labelHandler = Assert.IsType<LabelHandler>(label.Handler);
				Assert.NotNull(labelHandler.PlatformView);
				var labelPlatformView = labelHandler.PlatformView;

				using (var labelNode = labelPlatformView.CreateAccessibilityNodeInfo())
				{
					Assert.NotNull(labelNode);
					Assert.Equal(labelDescription, labelNode.ContentDescription?.ToString());
				}

				bool sampled = false;
				string entryContentDescription = "<not sampled>";
				string entryNodeText = "<not sampled>";

				await AssertEventually(
					() =>
					{
						using var entryNode = entryPlatformView.CreateAccessibilityNodeInfo();
						if (entryNode is null)
						{
							return false;
						}

						entryContentDescription = entryNode.ContentDescription?.ToString();
						entryNodeText = entryNode.Text?.ToString();
						sampled = true;

						return !string.IsNullOrEmpty(entryContentDescription) ||
							!string.IsNullOrEmpty(entryNodeText);
					},
					message: "Entry accessibility node should expose semantic content after attachment.");

				Assert.True(sampled, "Entry accessibility node should be sampled after attachment.");
				Assert.True(
					entryContentDescription == entryDescription,
					$"Entry accessibility content description should be '{entryDescription}'. " +
					$"Actual ContentDescription: '{entryContentDescription ?? "<null>"}'; " +
					$"node text: '{entryNodeText ?? "<null>"}'.");
			});
		}
	}
}
#endif

