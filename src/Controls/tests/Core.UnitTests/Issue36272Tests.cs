using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class Issue36272 : BaseTestFixture
	{
		[Fact, Category(TestCategory.Memory)]
		public async Task RemovedPickerWithRetainedItemsSourceIsCollectible()
		{
			var sharedItems = new ObservableCollection<string> { "a", "b", "c" };
			var pickerReference = CreateAndRemovePicker(sharedItems);

			var isAlive = await pickerReference.WaitForCollect();

			Assert.False(isAlive, "Picker remained alive after 40 GC cycles; expected IsAlive=False");
			GC.KeepAlive(sharedItems);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static WeakReference CreateAndRemovePicker(ObservableCollection<string> sharedItems)
		{
			var page = new ContentPage
			{
				Title = "Picker ItemsSource retention"
			};
			var root = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16
			};
			var pickerHost = new VerticalStackLayout();
			var picker = new Picker
			{
				ItemsSource = sharedItems
			};

			root.Children.Add(new Label
			{
				Text = "The default-styled Picker below uses a long-lived shared collection."
			});
			root.Children.Add(pickerHost);
			root.Children.Add(new Button
			{
				Text = "Unload Picker and check collection"
			});
			root.Children.Add(new Label
			{
				Text = "Ready to run"
			});
			root.Children.Add(new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = "NO BUG:"
			});
			page.Content = root;
			pickerHost.Children.Add(picker);

			Assert.Same(root, page.Content);
			Assert.Same(sharedItems, picker.ItemsSource);
			Assert.Equal(3, picker.Items.Count);
			Assert.Equal("a", picker.Items[0]);
			Assert.Equal("b", picker.Items[1]);
			Assert.Equal("c", picker.Items[2]);
			Assert.Same(pickerHost, picker.Parent);
			Assert.Contains(picker, pickerHost.Children);

			var removed = pickerHost.Children.Remove(picker);

			Assert.True(removed);
			Assert.DoesNotContain(picker, pickerHost.Children);
			Assert.Null(picker.Parent);

			return new WeakReference(picker);
		}
	}
}
