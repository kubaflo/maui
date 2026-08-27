#if ANDROID
using System.Threading.Tasks;
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
		public async Task EntryDescriptionMapsToNativeContentDescriptionAfterAttachment()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var entry = new Entry
			{
				AutomationId = "AutomationID_Entry",
				Placeholder = "Enter text here"
			};
			SemanticProperties.SetDescription(entry, "SemanticDecription_Entry");

			var label = new Label
			{
				AutomationId = "AutomationID_Label",
				Text = "Name",
				VerticalOptions = LayoutOptions.Start
			};
			SemanticProperties.SetDescription(label, "SemanticDescription_Label");

			var grid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Star }
				}
			};
			grid.Add(entry, 0, 0);
			grid.Add(label, 0, 1);

			var page = new ContentPage { Content = grid };
			const string sentinel = "<not observed>";
			var callbackOccurred = false;
			var entryNativeText = sentinel;
			var entryNativeDescription = sentinel;
			var labelNativeText = sentinel;
			var labelNativeDescription = sentinel;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var entryHandler = Assert.IsType<EntryHandler>(entry.Handler);
				var labelHandler = Assert.IsType<LabelHandler>(label.Handler);
				Assert.NotNull(entryHandler.PlatformView);
				Assert.NotNull(labelHandler.PlatformView);
				Assert.True(entryHandler.PlatformView.IsAttachedToWindow);
				Assert.True(labelHandler.PlatformView.IsAttachedToWindow);

				await AssertEventually(
					() => InvokeOnMainThreadAsync(() =>
					{
						using var entryNode = entryHandler.PlatformView.CreateAccessibilityNodeInfo();
						using var labelNode = labelHandler.PlatformView.CreateAccessibilityNodeInfo();
						Assert.NotNull(entryNode);
						Assert.NotNull(labelNode);

						entryNativeText = entryNode.Text?.ToString();
						entryNativeDescription = entryNode.ContentDescription?.ToString();
						labelNativeText = labelNode.Text?.ToString();
						labelNativeDescription = labelNode.ContentDescription?.ToString();
						callbackOccurred = true;
						return entryNativeText != sentinel &&
							entryNativeDescription != sentinel &&
							labelNativeText != sentinel &&
							labelNativeDescription != sentinel;
					}),
					message: "Timed out observing native semantics after attachment.");
			});

			Assert.True(callbackOccurred);
			Assert.Equal("AutomationID_Entry", entry.AutomationId);
			Assert.Equal("Enter text here", entry.Placeholder);
			Assert.Equal("SemanticDecription_Entry", SemanticProperties.GetDescription(entry));
			Assert.Equal("AutomationID_Label", label.AutomationId);
			Assert.Equal("Name", label.Text);
			Assert.Equal(LayoutOptions.Start, label.VerticalOptions);
			Assert.Equal("SemanticDescription_Label", SemanticProperties.GetDescription(label));
			Assert.Equal("Name", labelNativeText);
			Assert.Equal("SemanticDescription_Label", labelNativeDescription);

			Assert.True(
				entryNativeDescription == "SemanticDecription_Entry" && string.IsNullOrEmpty(entryNativeText),
				$"Entry native semantics mismatch after attachment: expected ContentDescription='SemanticDecription_Entry' and empty Text; observed ContentDescription='{entryNativeDescription}' and Text='{entryNativeText}'.");
		}
	}
}
#endif

