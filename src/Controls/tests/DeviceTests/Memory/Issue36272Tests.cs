#if MACCATALYST
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Xunit;

namespace Microsoft.Maui.DeviceTests.Memory;

[Category(TestCategory.Memory)]
[Category("Issue36272")]
public class Issue36272
{
	[Fact]
	public async Task SharedItemsSourceDoesNotRetainPicker()
	{
		var sharedItems = new ObservableCollection<string> { "Alpha", "Beta", "Gamma" };
		var pickerReference = CreatePickerReference(sharedItems);

		Assert.True(pickerReference.IsAlive);

		await AssertionExtensions.WaitForGC(pickerReference);

		GC.KeepAlive(sharedItems);

		[MethodImpl(MethodImplOptions.NoInlining)]
		static WeakReference CreatePickerReference(ObservableCollection<string> items)
		{
			var picker = new Picker
			{
				ItemsSource = items
			};
			var pickerReference = new WeakReference(picker);

			Assert.Same(picker, pickerReference.Target);
			Assert.Same(items, picker.ItemsSource);
			Assert.Equal(3, items.Count);
			Assert.Equal("Alpha", items[0]);
			Assert.Equal("Beta", items[1]);
			Assert.Equal("Gamma", items[2]);

			return pickerReference;
		}
	}
}
#endif

