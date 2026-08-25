#if WINDOWS
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WListView = Microsoft.UI.Xaml.Controls.ListView;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WVisibility = Microsoft.UI.Xaml.Visibility;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue32213")]
	public class Issue32213 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HeaderAndFooterTemplatesRenderTheirContent()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			bool collectionViewLoaded = false;
			bool headerTemplateLoaded = false;
			bool footerTemplateLoaded = false;
			var collectionView = new CollectionView
			{
				SelectionMode = SelectionMode.Single,
				ItemsSource = new[] { "1", "2", "3", "4" },
				Header = "Header",
				Footer = "Footer",
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label();
					label.SetBinding(Label.TextProperty, ".");
					return label;
				}),
				HeaderTemplate = new DataTemplate(() =>
				{
					var label = new Label { Text = "HeaderTemplate" };
					label.Loaded += (_, _) => headerTemplateLoaded = true;
					return label;
				}),
				FooterTemplate = new DataTemplate(() =>
				{
					var label = new Label { Text = "FooterTemplate" };
					label.Loaded += (_, _) => footerTemplateLoaded = true;
					return label;
				})
			};
			collectionView.Loaded += (_, _) => collectionViewLoaded = true;

			var page = new ContentPage
			{
				Content = collectionView
			};

			await CreateHandlerAndAddToWindow<CollectionViewHandler>(page, async handler =>
			{
				await AssertEventually(
					() => collectionViewLoaded,
					timeout: 5000,
					message: "Issue32213 CollectionView did not report Loaded after window attachment.");
				Assert.True(collectionViewLoaded);

				var nativeListView = handler.PlatformView as WListView;
				Assert.NotNull(nativeListView);

				await AssertEventually(
					() => nativeListView.IsLoaded && nativeListView.ActualWidth > 0 && nativeListView.ActualHeight > 0,
					timeout: 5000,
					message: "Issue32213 native ListView did not load and measure.");

				string[] GetVisibleNativeTexts() =>
					nativeListView
						.GetChildren<WTextBlock>()
						.Where(textBlock =>
							textBlock is not null &&
							textBlock.IsLoaded &&
							textBlock.Visibility == WVisibility.Visible &&
							textBlock.ActualWidth > 0 &&
							textBlock.ActualHeight > 0)
						.Select(textBlock => textBlock.Text)
						.Where(text => !string.IsNullOrEmpty(text))
						.ToArray();

				foreach (string item in new[] { "1", "2", "3", "4" })
				{
					await AssertEventually(
						() => GetVisibleNativeTexts().Contains(item),
						timeout: 5000,
						message: $"Issue32213 native item '{item}' did not render.");
				}

				await AssertEventually(
					() =>
						(headerTemplateLoaded || nativeListView.HeaderTemplate is null) &&
						(footerTemplateLoaded || nativeListView.FooterTemplate is null),
					timeout: 5000,
					message: "Issue32213 native header and footer template state did not settle.");

				Assert.True(
					headerTemplateLoaded && footerTemplateLoaded,
					$"Issue32213 native template rendering mismatch: " +
					$"headerTemplateLoaded={headerTemplateLoaded}, footerTemplateLoaded={footerTemplateLoaded}, " +
					$"nativeHeaderTemplate={nativeListView.HeaderTemplate is not null}, " +
					$"nativeFooterTemplate={nativeListView.FooterTemplate is not null}, " +
					$"observed=[{string.Join(", ", GetVisibleNativeTexts())}]");
			});
		}
	}
}
#endif

