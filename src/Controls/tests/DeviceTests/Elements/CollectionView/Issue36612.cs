using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue36612 : ControlsHandlerTestBase
{
#if ANDROID
	[Fact]
	[Category("Issue36612")]
	public async Task ReplacedItemsSourceAndRealizedItemsDoNotLeakAfterPop()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<NavigationPage, NavigationViewHandler>();
				handlers.AddHandler<Toolbar, ToolbarHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Grid, LayoutHandler>();
				handlers.AddHandler<CollectionView, CollectionViewHandler>();
				handlers.AddHandler<Label, LabelHandler>();
			});
		});

		var references = await RunCollectionViewScenario();

		await AssertionExtensions.WaitForGC(references);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	async Task<WeakReference[]> RunCollectionViewScenario()
	{
		var references = new List<WeakReference>();
		var realizedLabels = new List<Label>();
		var initialPage = new ContentPage();
		var navigationPage = new NavigationPage(initialPage);
		var collectionView = new CollectionView
		{
			Header = new Label { Text = "Header" },
			Footer = new Label { Text = "Footer" },
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, ".");
				realizedLabels.Add(label);
				return label;
			})
		};
		var collectionPage = new ContentPage { Content = collectionView };
		var window = new Window(navigationPage);

		await CreateHandlerAndAddToWindow<WindowHandlerStub>(window, async _ =>
		{
			await navigationPage.PushAsync(collectionPage);

			var originalSource = new ObservableCollection<string>
			{
				"Item 1",
				"Item 2",
				"Item 3"
			};
			collectionView.ItemsSource = originalSource;

			await AssertHelpers.AssertEventually(() => AreExpectedItemsRealized(realizedLabels));

			references.Add(new WeakReference(originalSource));
			AddRealizedItemReference(realizedLabels, references);

			bool poppedExpectedPage = false;
			navigationPage.Popped += (_, args) => poppedExpectedPage = ReferenceEquals(args.Page, collectionPage);

			collectionView.ItemsSource = new ObservableCollection<string>(originalSource);
			await navigationPage.PopAsync();
			await OnUnloadedAsync(collectionPage);

			Assert.True(poppedExpectedPage, "The CollectionView page should be reported by the pop event.");
			Assert.Same(initialPage, navigationPage.CurrentPage);
		});

		return references.ToArray();
	}

	static bool AreExpectedItemsRealized(List<Label> labels)
	{
		for (int itemIndex = 1; itemIndex <= 3; itemIndex++)
		{
			string expectedText = $"Item {itemIndex}";
			bool found = false;

			for (int labelIndex = 0; labelIndex < labels.Count; labelIndex++)
			{
				var label = labels[labelIndex];
				if (label.Text == expectedText && label.IsLoaded && label.Handler != null && label.Handler.PlatformView != null)
				{
					found = true;
					break;
				}
			}

			if (!found)
				return false;
		}

		return true;
	}

	static void AddRealizedItemReference(List<Label> labels, List<WeakReference> references)
	{
		for (int labelIndex = 0; labelIndex < labels.Count; labelIndex++)
		{
			var label = labels[labelIndex];
			if (label.Text != "Item 1")
				continue;

			references.Add(new WeakReference(label));
			return;
		}

		Assert.Fail("The first item label should have been realized.");
	}
#endif
}

