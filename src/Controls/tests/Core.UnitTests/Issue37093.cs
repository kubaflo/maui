using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class Issue37093
	{
		[Fact]
		public void RetainedChildKeepsTransientContentPageParentAlive()
		{
			var myPopupName = new Label
			{
				AutomationId = "MyPopupName",
				Text = "MyPopupName",
				FontSize = 18
			};
			var triggerButton = new Button
			{
				AutomationId = "TriggerButton",
				Text = "Trigger parent collection"
			};
			var grid = new Grid
			{
				Children = { myPopupName }
			};
			var stackLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					grid,
					triggerButton,
					new Label
					{
						AutomationId = "ResultLabel",
						Text = "NO BUG:",
						FontSize = 18
					}
				}
			};
			var originalPage = new ContentPage
			{
				Content = stackLayout
			};
			WeakReference<ContentPage> transientParentReference = null;
			var callbackCount = -1;

			triggerButton.Clicked += (sender, args) =>
			{
				callbackCount++;
				transientParentReference = AssignTransientParent(myPopupName);
			};

			Assert.Same(originalPage, stackLayout.Parent);
			Assert.Same(grid, myPopupName.RealParent);
			callbackCount = 0;
			((IButtonController)triggerButton).SendClicked();

			Assert.Equal(1, callbackCount);
			Assert.NotNull(transientParentReference);

			for (int iteration = 0; iteration < 3; iteration++)
			{
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
				GC.WaitForPendingFinalizers();
			}

			Assert.True(
				transientParentReference.TryGetTarget(out var transientParent),
				"The transient ContentPage parent should remain alive while MyPopupName is retained.");
			Assert.Same(transientParent, myPopupName.RealParent);
			GC.KeepAlive(myPopupName);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static WeakReference<ContentPage> AssignTransientParent(Label retainedLabel)
		{
			var transientParent = new ContentPage
			{
				Content = retainedLabel
			};

			Assert.Same(transientParent, retainedLabel.RealParent);
			return new WeakReference<ContentPage>(transientParent);
		}
	}
}
