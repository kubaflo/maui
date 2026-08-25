#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WButtonAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer;
using WIInvokeProvider = Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider;
using WListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;
using WPatternInterface = Microsoft.UI.Xaml.Automation.Peers.PatternInterface;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WVisibility = Microsoft.UI.Xaml.Visibility;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue24968")]
	public class Issue24968 : ControlsHandlerTestBase
	{
		const string FailureSignature = "Issue24968 template rendering mismatch after empty transition:";

		[Fact]
		public async Task TemplatesRemainVisibleAfterItemsSourceBecomesEmpty()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			Label headerLabel = null;
			Label itemLabel = null;
			Label emptyLabel = null;
			Label footerLabel = null;
			var emptyItems = Array.Empty<string>();
			var clickCount = -1;
			var observedNativeItemCount = -1;
			var postTriggerLayoutCount = -1;

			var scenarioDescriptionLabel = new Label
			{
				AutomationId = "ScenarioDescriptionLabel",
				Text = "Template rendering state"
			};

			var showEmptyButton = new Button
			{
				AutomationId = "ShowEmptyButton",
				Text = "Show empty collection"
			};

			var collectionView = new CollectionView
			{
				AutomationId = "CitiesCollection",
				ItemsSource = new[] { "Paris" },
				HeaderTemplate = new DataTemplate(() =>
				{
					headerLabel = new Label
					{
						AutomationId = "HeaderTemplateLabel",
						Text = "Cities header"
					};
					return headerLabel;
				}),
				ItemTemplate = new DataTemplate(() =>
				{
					itemLabel = new Label();
					itemLabel.SetBinding(Label.TextProperty, new Binding("."));
					return itemLabel;
				}),
				EmptyViewTemplate = new DataTemplate(() =>
				{
					emptyLabel = new Label
					{
						AutomationId = "EmptyTemplateLabel",
						Text = "No cities"
					};
					return emptyLabel;
				}),
				FooterTemplate = new DataTemplate(() =>
				{
					footerLabel = new Label
					{
						AutomationId = "FooterTemplateLabel",
						Text = "Cities footer"
					};
					return footerLabel;
				})
			};

			showEmptyButton.Clicked += (_, _) =>
			{
				clickCount++;
				collectionView.ItemsSource = emptyItems;
			};

			var controls = new VerticalStackLayout
			{
				Padding = 12,
				Spacing = 8,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						Text = "CollectionView template visibility"
					},
					scenarioDescriptionLabel,
					showEmptyButton
				}
			};

			var grid = new Grid
			{
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				},
				Children =
				{
					controls,
					collectionView
				}
			};
			Grid.SetRow(collectionView, 1);

			var page = new ContentPage { Content = grid };

			await CreateHandlerAndAddToWindow<PageHandler>(page, async _ =>
			{
				var collectionHandler = collectionView.Handler as CollectionViewHandler;
				Assert.NotNull(collectionHandler);
				var nativeCollection = collectionHandler.PlatformView as WListViewBase;
				Assert.NotNull(nativeCollection);

				var initialContentRendered = await Wait(() =>
					nativeCollection.Items.Count == 1 &&
					IsRendered(itemLabel, null, "Paris") &&
					IsRendered(headerLabel, "HeaderTemplateLabel", "Cities header") &&
					IsRendered(footerLabel, "FooterTemplateLabel", "Cities footer"),
					timeout: 5000);
				Assert.True(initialContentRendered,
					"Issue24968 setup mismatch: the populated item, header, and footer must render before the empty transition.");

				var nativeButton = showEmptyButton.Handler?.PlatformView as WButton;
				Assert.NotNull(nativeButton);
				var invokeProvider = new WButtonAutomationPeer(nativeButton)
					.GetPattern(WPatternInterface.Invoke) as WIInvokeProvider;
				Assert.NotNull(invokeProvider);

				var transitionStarted = false;
				nativeCollection.LayoutUpdated += OnCollectionLayoutUpdated;
				transitionStarted = true;
				invokeProvider.Invoke();

				var clickObserved = await Wait(() =>
					clickCount != -1 &&
					ReferenceEquals(collectionView.ItemsSource, emptyItems),
					timeout: 3000);
				Assert.True(clickObserved,
					"Issue24968 setup mismatch: the attached native button did not invoke the MAUI Clicked handler.");

				var emptyStateApplied = await Wait(() =>
				{
					observedNativeItemCount = nativeCollection.Items.Count;
					return observedNativeItemCount == 0 && postTriggerLayoutCount != -1;
				}, timeout: 3000);
				nativeCollection.LayoutUpdated -= OnCollectionLayoutUpdated;
				Assert.True(emptyStateApplied,
					$"Issue24968 setup mismatch: native item count was {observedNativeItemCount} and layout count was {postTriggerLayoutCount}; expected the empty source to complete a native layout pass.");

				Assert.True(
					IsRendered(headerLabel, "HeaderTemplateLabel", "Cities header") &&
					IsRendered(emptyLabel, "EmptyTemplateLabel", "No cities") &&
					IsRendered(footerLabel, "FooterTemplateLabel", "Cities footer"),
					$"{FailureSignature} " +
					DescribeState("header", headerLabel, "HeaderTemplateLabel", "Cities header") + " " +
					DescribeState("empty view", emptyLabel, "EmptyTemplateLabel", "No cities") + " " +
					DescribeState("footer", footerLabel, "FooterTemplateLabel", "Cities footer"));

				void OnCollectionLayoutUpdated(object sender, object args)
				{
					if (transitionStarted)
						postTriggerLayoutCount++;
				}
			});
		}

		static bool IsRendered(Label label, string expectedAutomationId, string expectedText)
		{
			var nativeLabel = GetNativeLabel(label);
			return label is not null &&
				(expectedAutomationId is null || label.AutomationId == expectedAutomationId) &&
				nativeLabel is not null &&
				nativeLabel.Text == expectedText &&
				nativeLabel.IsLoaded &&
				nativeLabel.Visibility == WVisibility.Visible &&
				nativeLabel.ActualWidth > 0.5 &&
				nativeLabel.ActualHeight > 0.5;
		}

		static WTextBlock GetNativeLabel(Label label) =>
			label?.Handler?.PlatformView as WTextBlock;

		static string DescribeState(string name, Label label, string expectedAutomationId, string expectedText)
		{
			var nativeLabel = GetNativeLabel(label);
			var actualAutomationId = label?.AutomationId ?? "<missing>";
			var actualText = nativeLabel?.Text ?? "<missing>";
			var loaded = nativeLabel?.IsLoaded.ToString() ?? "<missing>";
			var visibility = nativeLabel?.Visibility.ToString() ?? "<missing>";
			var width = nativeLabel?.ActualWidth.ToString() ?? "<missing>";
			var height = nativeLabel?.ActualHeight.ToString() ?? "<missing>";

			return $"{FailureSignature} {name} identity={actualAutomationId}, text={actualText}, " +
				$"loaded={loaded}, visibility={visibility}, width={width}, height={height}; " +
				$"expected identity={expectedAutomationId}, text={expectedText}, loaded=True, " +
				"visibility=Visible, width>0.5, height>0.5.";
		}
	}
}
#endif

