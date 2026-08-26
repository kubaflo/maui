#if WINDOWS
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue24968")]
	public class Issue24968 : ControlsHandlerTestBase
	{
		const double LargeFontSize = 32;
		const double GeometryTolerance = 2;

		[Fact]
		public async Task EmptyViewTemplateDoesNotOverlapHeaderOrFooter()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var populatedScenario = CreateScenario(["Calibration item"]);
			await CreateHandlerAndAddToWindow<PageHandler>(populatedScenario.TestPage, async _ =>
			{
				await AssertEventually(
					() => IsPopulatedScenarioReady(populatedScenario),
					timeout: 5000,
					message: "Issue24968 calibration templates were not loaded and measured.");

				AssertTemplateContract(populatedScenario, expectEmptySource: false);

				var viewport = Assert.IsAssignableFrom<CollectionViewHandler>(populatedScenario.Collection.Handler).PlatformView;
				NativeFrame headerFrame = GetFrame(populatedScenario.HeaderLabel, populatedScenario.Collection);
				NativeFrame itemFrame = GetFrame(populatedScenario.ItemLabel, populatedScenario.Collection);
				NativeFrame footerFrame = GetFrame(populatedScenario.FooterLabel, populatedScenario.Collection);

				Assert.True(Math.Abs(headerFrame.Y) <= GeometryTolerance,
					$"Issue24968 calibration header did not start at the viewport top: {Describe(headerFrame)}.");
				AssertFramesAreVisibleAndOrdered(
					viewport.ActualWidth,
					viewport.ActualHeight,
					"calibration",
					headerFrame,
					itemFrame,
					footerFrame);

				AssertHeightMatchesDesired(populatedScenario.HeaderLabel, headerFrame, "header");
				AssertHeightMatchesDesired(populatedScenario.ItemLabel, itemFrame, "item");
				AssertHeightMatchesDesired(populatedScenario.FooterLabel, footerFrame, "footer");
			});

			var emptyScenario = CreateScenario([]);
			await CreateHandlerAndAddToWindow<PageHandler>(emptyScenario.TestPage, async _ =>
			{
				await AssertEventually(
					() => IsEmptyScenarioReady(emptyScenario),
					timeout: 5000,
					message: "Issue24968 empty templates were not loaded and measured.");

				AssertTemplateContract(emptyScenario, expectEmptySource: true);

				var viewport = Assert.IsAssignableFrom<CollectionViewHandler>(emptyScenario.Collection.Handler).PlatformView;
				NativeFrame headerFrame = GetFrame(emptyScenario.HeaderLabel, emptyScenario.Collection);
				NativeFrame emptyFrame = GetFrame(emptyScenario.EmptyLabel, emptyScenario.Collection);
				NativeFrame footerFrame = GetFrame(emptyScenario.FooterLabel, emptyScenario.Collection);

				bool framesAreVisible =
					IsInsideViewport(headerFrame, viewport.ActualWidth, viewport.ActualHeight) &&
					IsInsideViewport(emptyFrame, viewport.ActualWidth, viewport.ActualHeight) &&
					IsInsideViewport(footerFrame, viewport.ActualWidth, viewport.ActualHeight);
				bool framesDoNotOverlap =
					Bottom(headerFrame) <= emptyFrame.Y + GeometryTolerance &&
					Bottom(emptyFrame) <= footerFrame.Y + GeometryTolerance;

				Assert.True(
					framesAreVisible && framesDoNotOverlap,
					$"Issue24968 template layout overlap: header={Describe(headerFrame)}, empty={Describe(emptyFrame)}, " +
					$"footer={Describe(footerFrame)}, viewport=(0,0,{viewport.ActualWidth:0.##},{viewport.ActualHeight:0.##}), " +
					$"tolerance={GeometryTolerance:0.##}; expected positive in-viewport frames ordered header, empty, footer without overlap.");
			});
		}

		static Scenario CreateScenario(ObservableCollection<string> cities)
		{
			var scenario = new Scenario { Cities = cities };

			scenario.HeaderTemplate = new DataTemplate(() =>
			{
				scenario.HeaderCreated++;
				var label = new Label
				{
					Text = "Cities",
					FontAttributes = FontAttributes.Bold,
					FontSize = LargeFontSize,
					Padding = 10
				};
				TrackLabel(label, scenario, TemplateKind.Header);
				return label;
			});

			scenario.ItemTemplate = new DataTemplate(() =>
			{
				scenario.ItemCreated++;
				var label = new Label { Padding = new Thickness(20, 5, 5, 5) };
				label.SetBinding(Label.TextProperty, ".");
				TrackLabel(label, scenario, TemplateKind.Item);
				return label;
			});

			scenario.EmptyTemplate = new DataTemplate(() =>
			{
				scenario.EmptyCreated++;
				var label = new Label
				{
					Text = "Empty",
					Padding = new Thickness(20, 5, 5, 5)
				};
				TrackLabel(label, scenario, TemplateKind.Empty);
				return label;
			});

			scenario.FooterTemplate = new DataTemplate(() =>
			{
				scenario.FooterCreated++;
				var label = new Label
				{
					Text = "Hello world !!!",
					FontAttributes = FontAttributes.Bold,
					FontSize = LargeFontSize,
					Padding = 10
				};
				TrackLabel(label, scenario, TemplateKind.Footer);
				return label;
			});

			scenario.Collection = new CollectionView
			{
				HeaderTemplate = scenario.HeaderTemplate,
				ItemTemplate = scenario.ItemTemplate,
				EmptyViewTemplate = scenario.EmptyTemplate,
				FooterTemplate = scenario.FooterTemplate
			};
			scenario.Collection.SetBinding(ItemsView.ItemsSourceProperty, "Cities");

			var grid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};

			var titleLabel = new Label
			{
				Text = "Empty CollectionView template layout",
				FontAttributes = FontAttributes.Bold,
				Padding = 10
			};
			var checkButton = new Button { Text = "Check template layout" };
			var resultLabel = new Label
			{
				Text = "Template layout status",
				FontAttributes = FontAttributes.Bold,
				Padding = 10
			};

			Grid.SetRow(scenario.Collection, 1);
			Grid.SetRow(checkButton, 2);
			Grid.SetRow(resultLabel, 3);
			grid.Children.Add(titleLabel);
			grid.Children.Add(scenario.Collection);
			grid.Children.Add(checkButton);
			grid.Children.Add(resultLabel);

			var testPage = new Issue24968Page
			{
				Cities = cities,
				Content = grid
			};
			testPage.BindingContext = testPage;
			scenario.TestPage = testPage;

			return scenario;
		}

		static void TrackLabel(Label label, Scenario scenario, TemplateKind kind)
		{
			switch (kind)
			{
				case TemplateKind.Header:
					scenario.HeaderLabel = label;
					label.Loaded += (_, _) => scenario.HeaderLoaded++;
					label.SizeChanged += (_, _) => scenario.HeaderSized++;
					break;
				case TemplateKind.Item:
					scenario.ItemLabel = label;
					label.Loaded += (_, _) => scenario.ItemLoaded++;
					label.SizeChanged += (_, _) => scenario.ItemSized++;
					break;
				case TemplateKind.Empty:
					scenario.EmptyLabel = label;
					label.Loaded += (_, _) => scenario.EmptyLoaded++;
					label.SizeChanged += (_, _) => scenario.EmptySized++;
					break;
				case TemplateKind.Footer:
					scenario.FooterLabel = label;
					label.Loaded += (_, _) => scenario.FooterLoaded++;
					label.SizeChanged += (_, _) => scenario.FooterSized++;
					break;
			}
		}
		static bool IsPopulatedScenarioReady(Scenario scenario) =>
			scenario.HeaderCreated > 0 &&
			scenario.ItemCreated > 0 &&
			scenario.FooterCreated > 0 &&
			scenario.HeaderLoaded > 0 &&
			scenario.ItemLoaded > 0 &&
			scenario.FooterLoaded > 0 &&
			scenario.HeaderSized > 0 &&
			scenario.ItemSized > 0 &&
			scenario.FooterSized > 0 &&
			IsNativeElementMeasured(scenario.Collection) &&
			IsNativeElementMeasured(scenario.HeaderLabel) &&
			IsNativeElementMeasured(scenario.ItemLabel) &&
			IsNativeElementMeasured(scenario.FooterLabel);

		static bool IsEmptyScenarioReady(Scenario scenario) =>
			scenario.HeaderCreated > 0 &&
			scenario.EmptyCreated > 0 &&
			scenario.FooterCreated > 0 &&
			scenario.HeaderLoaded > 0 &&
			scenario.EmptyLoaded > 0 &&
			scenario.FooterLoaded > 0 &&
			scenario.HeaderSized > 0 &&
			scenario.EmptySized > 0 &&
			scenario.FooterSized > 0 &&
			IsNativeElementMeasured(scenario.Collection) &&
			IsNativeElementMeasured(scenario.HeaderLabel) &&
			IsNativeElementMeasured(scenario.EmptyLabel) &&
			IsNativeElementMeasured(scenario.FooterLabel);

		static bool IsNativeElementMeasured(VisualElement element)
		{
			if (element is Label label && label.Handler is LabelHandler labelHandler)
				return labelHandler.PlatformView.IsLoaded &&
					labelHandler.PlatformView.ActualWidth > 0 &&
					labelHandler.PlatformView.ActualHeight > 0;

			if (element is CollectionView collection && collection.Handler is CollectionViewHandler collectionHandler)
				return collectionHandler.PlatformView.IsLoaded &&
					collectionHandler.PlatformView.ActualWidth > 0 &&
					collectionHandler.PlatformView.ActualHeight > 0;

			return false;
		}

		static void AssertTemplateContract(Scenario scenario, bool expectEmptySource)
		{
			Assert.Same(scenario.Cities, scenario.Collection.ItemsSource);
			Assert.Equal(expectEmptySource ? 0 : 1, scenario.Cities.Count);
			Assert.Same(scenario.HeaderTemplate, scenario.Collection.HeaderTemplate);
			Assert.Same(scenario.ItemTemplate, scenario.Collection.ItemTemplate);
			Assert.Same(scenario.EmptyTemplate, scenario.Collection.EmptyViewTemplate);
			Assert.Same(scenario.FooterTemplate, scenario.Collection.FooterTemplate);

			Assert.Equal("Cities", scenario.HeaderLabel.Text);
			Assert.Equal(FontAttributes.Bold, scenario.HeaderLabel.FontAttributes);
			Assert.Equal(LargeFontSize, scenario.HeaderLabel.FontSize);
			Assert.Equal(new Thickness(10), scenario.HeaderLabel.Padding);

			Assert.Equal("Hello world !!!", scenario.FooterLabel.Text);
			Assert.Equal(FontAttributes.Bold, scenario.FooterLabel.FontAttributes);
			Assert.Equal(LargeFontSize, scenario.FooterLabel.FontSize);
			Assert.Equal(new Thickness(10), scenario.FooterLabel.Padding);

			if (expectEmptySource)
			{
				Assert.Equal("Empty", scenario.EmptyLabel.Text);
				Assert.Equal(new Thickness(20, 5, 5, 5), scenario.EmptyLabel.Padding);
			}
			else
			{
				Assert.Equal("Calibration item", scenario.ItemLabel.Text);
				Assert.Equal(new Thickness(20, 5, 5, 5), scenario.ItemLabel.Padding);
			}
		}
		static NativeFrame GetFrame(Label label, CollectionView collection)
		{
			Assert.NotNull(label);
			Assert.NotNull(collection);
			var platformElement = Assert.IsAssignableFrom<LabelHandler>(label.Handler).PlatformView;
			var viewport = Assert.IsAssignableFrom<CollectionViewHandler>(collection.Handler).PlatformView;
			var location = platformElement.GetLocationRelativeTo(viewport);
			Assert.True(location.HasValue, "Issue24968 native label location was unavailable.");
			return new NativeFrame(location.Value.X, location.Value.Y, platformElement.ActualWidth, platformElement.ActualHeight);
		}

		static void AssertFramesAreVisibleAndOrdered(
			double viewportWidth,
			double viewportHeight,
			string scenarioName,
			NativeFrame first,
			NativeFrame second,
			NativeFrame third)
		{
			bool visible =
				IsInsideViewport(first, viewportWidth, viewportHeight) &&
				IsInsideViewport(second, viewportWidth, viewportHeight) &&
				IsInsideViewport(third, viewportWidth, viewportHeight);
			bool ordered =
				Bottom(first) <= second.Y + GeometryTolerance &&
				Bottom(second) <= third.Y + GeometryTolerance;

			Assert.True(visible && ordered,
				$"Issue24968 {scenarioName} frame oracle failed: first={Describe(first)}, second={Describe(second)}, " +
				$"third={Describe(third)}, viewport=(0,0,{viewportWidth:0.##},{viewportHeight:0.##}).");
		}

		static void AssertHeightMatchesDesired(Label label, NativeFrame frame, string templateName)
		{
			Assert.NotNull(label);
			var platformElement = Assert.IsAssignableFrom<LabelHandler>(label.Handler).PlatformView;
			Assert.True(platformElement.DesiredSize.Height > 0,
				$"Issue24968 calibration {templateName} had no desired height.");
			Assert.True(Math.Abs(frame.Height - platformElement.DesiredSize.Height) <= GeometryTolerance,
				$"Issue24968 calibration {templateName} actual height {frame.Height:0.##} did not match desired height {platformElement.DesiredSize.Height:0.##}.");
		}

		static bool IsInsideViewport(NativeFrame frame, double viewportWidth, double viewportHeight) =>
			frame.Width > 0 &&
			frame.Height > 0 &&
			frame.X >= -GeometryTolerance &&
			frame.Y >= -GeometryTolerance &&
			frame.X + frame.Width <= viewportWidth + GeometryTolerance &&
			Bottom(frame) <= viewportHeight + GeometryTolerance;

		static double Bottom(NativeFrame frame) => frame.Y + frame.Height;

		static string Describe(NativeFrame frame) =>
			FormattableString.Invariant($"({frame.X:0.##},{frame.Y:0.##},{frame.Width:0.##},{frame.Height:0.##})");

		readonly struct NativeFrame
		{
			public NativeFrame(double x, double y, double width, double height)
			{
				X = x;
				Y = y;
				Width = width;
				Height = height;
			}

			public double X { get; }
			public double Y { get; }
			public double Width { get; }
			public double Height { get; }
		}

		enum TemplateKind
		{
			Header,
			Item,
			Empty,
			Footer
		}

		sealed class Issue24968Page : ContentPage
		{
			public ObservableCollection<string> Cities { get; set; }
		}

		sealed class Scenario
		{
			public ObservableCollection<string> Cities;
			public ContentPage TestPage;
			public CollectionView Collection;
			public DataTemplate HeaderTemplate;
			public DataTemplate ItemTemplate;
			public DataTemplate EmptyTemplate;
			public DataTemplate FooterTemplate;
			public Label HeaderLabel;
			public Label ItemLabel;
			public Label EmptyLabel;
			public Label FooterLabel;
			public int HeaderCreated;
			public int ItemCreated;
			public int EmptyCreated;
			public int FooterCreated;
			public int HeaderLoaded;
			public int ItemLoaded;
			public int EmptyLoaded;
			public int FooterLoaded;
			public int HeaderSized;
			public int ItemSized;
			public int EmptySized;
			public int FooterSized;
		}
	}
}
#endif

