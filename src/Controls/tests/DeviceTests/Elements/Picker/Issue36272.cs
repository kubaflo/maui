#if MACCATALYST
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36272")]
	public class Issue36272 : ControlsHandlerTestBase
	{
		const int PickerCount = 5;

		[Fact]
		public async Task SharedItemsSourceDoesNotRetainReleasedPickers()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Picker, PickerHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var sharedItems = new ObservableCollection<string> { "a", "b", "c" };
			var referencePicker = new Picker { ItemsSource = sharedItems };
			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Children =
						{
							new Label { Text = "Shared Picker ItemsSource leak" },
							new Label { Text = "The Picker below shares its ItemsSource with temporary Pickers." },
							referencePicker,
							new Label { Text = "Released Pickers" },
							new Label { Text = "Collection pending" },
							new Button { Text = "Create, release, and collect 5 Pickers" }
						}
					}
				}
			};

			var releasedCount = -1;
			WeakReference[] pickerReferences = [];

			await CreateHandlerAndAddToWindow(page, () =>
			{
				Assert.IsType<PickerHandler>(referencePicker.Handler);
				pickerReferences = CreatePickerReferences(sharedItems, out releasedCount);
			});

			Assert.Equal(PickerCount, releasedCount);
			Assert.Equal(PickerCount, pickerReferences.Length);
			await AssertionExtensions.WaitForGC(pickerReferences);
			GC.KeepAlive(sharedItems);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static WeakReference[] CreatePickerReferences(ObservableCollection<string> sharedItems, out int releasedCount)
		{
			var references = new WeakReference[PickerCount];

			for (var index = 0; index < references.Length; index++)
			{
				var picker = new Picker { ItemsSource = sharedItems };
				references[index] = new WeakReference(picker);
			}

			releasedCount = references.Count(reference => reference.IsAlive);
			Assert.Equal(PickerCount, references.Select(reference => reference.Target).Distinct().Count());
			return references;
		}
	}
}
#endif

