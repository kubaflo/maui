using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Android.OS;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests.Memory;

[Category(TestCategory.Memory)]
[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue35697 : ControlsHandlerTestBase
{
	[Fact]
	public async Task RemovedVisualTreesAreCollected()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<Entry, EntryHandler>();
				handlers.AddHandler<Border, BorderHandler>();
			});
		});

		var probeHost = new Grid();
		var probeBorder = new Border
		{
			HeightRequest = 120,
			Content = probeHost
		};
		var rootLayout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label { Text = "Android lifecycle retention probe", FontSize = 24 },
				new Label { Text = "Creates and removes complete MAUI visual trees to check whether discarded controls remain retained." },
				new Button { Text = "Create and remove visual tree" },
				new Button { Text = "Check discarded trees" },
				new Label { Text = "Discarded trees: 0" },
				probeBorder,
				new Label { Text = "NO BUG: discarded visual trees have not been found retained" }
			}
		};
		var page = new ContentPage { Content = rootLayout };
		var probes = new List<DiscardedTreeProbe>();
		var cycleCountLabel = (Label)rootLayout.Children[4];

		await CreateHandlerAndAddToWindow(page, () =>
		{
			Assert.Same(Looper.MainLooper, Looper.MyLooper());
			Assert.True(page.IsLoaded);
			Assert.Equal(new Thickness(24), rootLayout.Padding);
			Assert.Equal(16, rootLayout.Spacing);
			Assert.Equal(120, probeBorder.HeightRequest);
			Assert.Equal(120, probeBorder.Height);

			for (int index = 1; index <= 5; index++)
			{
				probes.Add(AddAndRemoveTree(probeHost, index));
				cycleCountLabel.Text = $"Discarded trees: {index}";
			}

			Assert.Empty(probeHost.Children);
			Assert.Equal(5, probes.Count);
			Assert.Equal("Discarded trees: 5", cycleCountLabel.Text);

			int retainedCount = -1;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			retainedCount = probes.Count(probe => probe.TreeReference.IsAlive);

			Assert.NotEqual(-1, retainedCount);
			foreach (var probe in probes)
			{
				Assert.False(
					probe.TreeReference.IsAlive,
					$"Discarded visual tree remained alive after removal and immediate garbage collection. Probe {probe.Index}, managed identity {probe.ManagedIdentity}, native identity {probe.NativeIdentity}, retained count {retainedCount}.");
			}
			Assert.Equal(0, retainedCount);
		});
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	DiscardedTreeProbe AddAndRemoveTree(Grid probeHost, int index)
	{
		var tree = new VerticalStackLayout
		{
			Children =
			{
				new Label { Text = $"Visual tree {index}" },
				new Entry { Text = "Lifecycle probe" },
				new Button { Text = "Probe child" }
			}
		};

		probeHost.Children.Add(tree);

		Assert.Equal($"Visual tree {index}", ((Label)tree.Children[0]).Text);
		Assert.Equal("Lifecycle probe", ((Entry)tree.Children[1]).Text);
		Assert.False(((Entry)tree.Children[1]).IsFocused);
		Assert.Equal("Probe child", ((Button)tree.Children[2]).Text);

		var nativeView = Assert.IsAssignableFrom<AView>(tree.Handler.PlatformView);
		Assert.NotNull(nativeView.Parent);
		int managedIdentity = RuntimeHelpers.GetHashCode(tree);
		int nativeIdentity = RuntimeHelpers.GetHashCode(nativeView);

		probeHost.Children.Remove(tree);

		Assert.Same(nativeView, tree.Handler.PlatformView);
		Assert.Null(nativeView.Parent);

		return new DiscardedTreeProbe(index, managedIdentity, nativeIdentity, new WeakReference(tree));
	}

	sealed record DiscardedTreeProbe(int Index, int ManagedIdentity, int NativeIdentity, WeakReference TreeReference);
}
