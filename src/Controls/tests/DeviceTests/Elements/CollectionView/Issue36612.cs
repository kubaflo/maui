#if ANDROID
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue36612")]
[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue36612 : ControlsHandlerTestBase
{
	[Fact]
	public async Task ReplacingItemsSourceAndPoppingPageReleasesRealizedItems()
	{
		var references = await CreateLeakReferences();

		await AssertionExtensions.WaitForGC(references);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	async Task<WeakReference[]> CreateLeakReferences()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Toolbar, ToolbarHandler>();
				handlers.AddHandler<NavigationPage, NavigationViewHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<CollectionView, CollectionViewHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Grid, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
			});
		});

		var references = new List<WeakReference>();
		var labels = new List<Label>();
		var loadedLabels = new HashSet<Label>();
		var loadedCompletion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var observedRealizedCount = -1;

		void OnItemLoaded(object sender, EventArgs _)
		{
			if (sender is Label label && loadedLabels.Add(label) && loadedLabels.Count == 3)
			{
				observedRealizedCount = labels.Count;
				loadedCompletion.TrySetResult(observedRealizedCount);
			}
		}

		var collectionView = new CollectionView
		{
			Header = new Label { Text = "Header" },
			Footer = new Label { Text = "Footer" },
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, ".");
				label.Loaded += OnItemLoaded;
				labels.Add(label);
				return label;
			})
		};
		var rootPage = new ContentPage();
		var collectionPage = new ContentPage { Content = collectionView };
		var navigationPage = new NavigationPage(rootPage);

		await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(navigationPage), async _ =>
		{
			await navigationPage.PushAsync(collectionPage);

			var source = new ObservableCollection<string>
			{
				"Item 1",
				"Item 2",
				"Item 3"
			};

			collectionView.ItemsSource = source;
			await loadedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));

			foreach (var label in labels)
				label.Loaded -= OnItemLoaded;

			Assert.Equal(3, observedRealizedCount);
			Assert.Same(source, collectionView.ItemsSource);
			Assert.Equal(
				new[] { "Item 1", "Item 2", "Item 3" },
				labels.Select(label => label.Text).OrderBy(text => text).ToArray());

			references.Add(new WeakReference(source));
			foreach (var label in labels)
			{
				Assert.NotNull(label.Handler);
				Assert.NotNull(label.Handler.PlatformView);
				references.Add(new WeakReference(label));
				references.Add(new WeakReference(label.Handler));
				references.Add(new WeakReference(label.Handler.PlatformView));
			}

			var replacementSource = new ObservableCollection<string>(source);
			collectionView.ItemsSource = replacementSource;
			await AssertEventually(() => ReferenceEquals(collectionView.ItemsSource, replacementSource));

			labels.Clear();
			var poppedPage = await navigationPage.PopAsync();

			Assert.Same(collectionPage, poppedPage);
			Assert.Single(navigationPage.Navigation.NavigationStack);
			Assert.Same(rootPage, navigationPage.CurrentPage);
			await AssertEventually(() => collectionPage.Parent is null && !collectionPage.IsLoaded);
		});

		return references.ToArray();
	}
}
#endif

