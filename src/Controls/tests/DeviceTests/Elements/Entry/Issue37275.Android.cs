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
	[Category(TestCategory.Entry)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37275 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task PublicEntryHandlerMapperRunsForMaterial3Entry()
		{
			const string mapperKey = "Issue37275";

			Entry entry = null;
			Entry loadedEntry = null;
			IEntry mapperObservedEntry = null;
			int mapperCallbackCount = 0;

			try
			{
				EntryHandler.Mapper.AppendToMapping(mapperKey, (_, view) =>
				{
					if (ReferenceEquals(view, entry))
					{
						mapperCallbackCount++;
						mapperObservedEntry = view;
					}
				});

				EnsureHandlerCreated(builder =>
				{
					builder.ConfigureMauiHandlers(handlers =>
					{
						handlers.AddHandler<Window, WindowHandlerStub>();
						handlers.AddHandler<ContentPage, PageHandler>();
						handlers.AddHandler<ScrollView, ScrollViewHandler>();
						handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
						handlers.AddHandler<Label, LabelHandler2>();
						handlers.AddHandler<Entry, EntryHandler2>();
					});
				});

				entry = new Entry
				{
					Text = "Material3 mapper probe"
				};
				entry.Loaded += (_, _) => loadedEntry = entry;

				var content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							Text = "EntryHandler Material3 mapper probe",
							FontSize = 22
						},
						new Label
						{
							Text = "The Entry below retains its default Material3 styling while its handler is created."
						},
						entry,
						new Label
						{
							Text = "Expected: the EntryHandler.Mapper callback is called during handler creation."
						},
						new Label
						{
							Text = "NO BUG:",
							FontAttributes = FontAttributes.Bold,
							FontSize = 18
						}
					}
				};

				var page = new ContentPage
				{
					Content = new ScrollView
					{
						Content = content
					}
				};

				await CreateHandlerAndAddToWindow(page, () =>
				{
					Assert.NotNull(loadedEntry);
					Assert.Same(entry, loadedEntry);
					Assert.Equal("Material3 mapper probe", entry.Text);
					Assert.Null(entry.Style);

					var handler = Assert.IsType<EntryHandler2>(entry.Handler);
					Assert.IsType<MauiMaterialTextInputLayout>(handler.PlatformView);

					Assert.True(
						mapperCallbackCount == 1,
						$"EntryHandler.Mapper callback count should be 1 after the Entry loaded, but was {mapperCallbackCount}; resolved handler was {handler.GetType().Name}.");

					Assert.Same(entry, mapperObservedEntry);
				});
			}
			finally
			{
				EntryHandler.Mapper.Add(mapperKey, static (_, _) => { });
			}
		}
	}
}
