using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class Issue37243 : BaseTestFixture
	{
		[Fact, Category(TestCategory.Memory)]
		public async Task RemovedMultiBindingTargetCanBeCollected()
		{
			var source = new BindingSource();
			var sharedBinding = new MultiBinding
			{
				StringFormat = "{0}",
				Bindings =
				{
					new Binding
					{
						Path = nameof(BindingSource.Value),
						Source = source
					}
				}
			};
			var targetHost = new VerticalStackLayout();
			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children = { targetHost }
			};
			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = content
				}
			};

			var targetReference = AddAndRemoveTarget(targetHost, sharedBinding);
			var collectionState = "<not-observed>";
			var isAlive = await targetReference.WaitForCollect();
			collectionState = isAlive ? "alive" : "collected";

			GC.KeepAlive(sharedBinding);
			GC.KeepAlive(page);

			Assert.NotEqual("<not-observed>", collectionState);
			Assert.True(
				collectionState == "collected",
				$"Removed MultiBinding target collection state: observed={collectionState}; expected=collected.");
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static WeakReference AddAndRemoveTarget(VerticalStackLayout targetHost, MultiBinding sharedBinding)
		{
			var target = new Label();
			target.SetBinding(Label.TextProperty, sharedBinding);
			targetHost.Children.Add(target);

			Assert.Same(target, targetHost.Children[0]);
			Assert.Equal("Bound target before removal", target.Text);

			var targetReference = new WeakReference(target);
			Assert.True(targetHost.Children.Remove(target));
			Assert.DoesNotContain(target, targetHost.Children);
			Assert.Null(target.Parent);

			return targetReference;
		}

		sealed class BindingSource
		{
			public string Value => "Bound target before removal";
		}
	}
}
