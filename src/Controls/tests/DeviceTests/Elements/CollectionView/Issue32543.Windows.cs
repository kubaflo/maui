using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue32543")]
	public class Issue32543 : ControlsHandlerTestBase
	{
		const double ItemHeight = 25;
		const double Tolerance = 2;
		const int RealizationTimeout = 5000;

		[Fact]
		public async Task HorizontalItemsHonorVerticalOptions()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var calibration = CreateCalibrationScene();
			await CreateHandlerAndAddToWindow<IWindowHandler>(calibration.Page, async _ =>
			{
				await WaitForLoadedAndSized(calibration.StartLabel, () => calibration.StartLoaded);
				await WaitForLoadedAndSized(calibration.CenterLabel, () => calibration.CenterLoaded);
				await WaitForLoadedAndSized(calibration.EndLabel, () => calibration.EndLoaded);

				var middle = GetNativeBounds(calibration.Middle);
				var startLabel = GetNativeBounds(calibration.StartLabel);
				var centerLabel = GetNativeBounds(calibration.CenterLabel);
				var endLabel = GetNativeBounds(calibration.EndLabel);

				AssertAlignment("Calibration Start", middle.Top, startLabel.Top);
				AssertAlignment("Calibration Center", middle.Center, centerLabel.Center);
				AssertAlignment("Calibration End", middle.Bottom, endLabel.Bottom);
			});

			var reported = CreateReportedScene();
			await CreateHandlerAndAddToWindow<IWindowHandler>(reported.Page, async _ =>
			{
				await WaitForLoadedAndSized(reported.StartCollection, () => reported.StartCollectionLoaded);
				await WaitForLoadedAndSized(reported.CenterCollection, () => reported.CenterCollectionLoaded);
				await WaitForLoadedAndSized(reported.EndCollection, () => reported.EndCollectionLoaded);
				await AssertEventually(() => reported.StartLabel != null, timeout: RealizationTimeout);
				await AssertEventually(() => reported.CenterLabel != null, timeout: RealizationTimeout);
				await AssertEventually(() => reported.EndLabel != null, timeout: RealizationTimeout);
				await WaitForLoadedAndSized(reported.StartLabel, () => reported.StartLabelLoaded);
				await WaitForLoadedAndSized(reported.CenterLabel, () => reported.CenterLabelLoaded);
				await WaitForLoadedAndSized(reported.EndLabel, () => reported.EndLabelLoaded);

				AssertItem(reported.StartLabel, "Item VerticalOptions=\"Start\"", LayoutOptions.Start);
				AssertItem(reported.CenterLabel, "Item VerticalOptions=\"Center\"", LayoutOptions.Center);
				AssertItem(reported.EndLabel, "Item VerticalOptions=\"End\"", LayoutOptions.End);

				var startCollection = GetNativeBounds(reported.StartCollection);
				var centerCollection = GetNativeBounds(reported.CenterCollection);
				var endCollection = GetNativeBounds(reported.EndCollection);
				var startLabel = GetNativeBounds(reported.StartLabel);
				var centerLabel = GetNativeBounds(reported.CenterLabel);
				var endLabel = GetNativeBounds(reported.EndLabel);

				Assert.True(
					Math.Abs(startLabel.Top - startCollection.Top) <= Tolerance,
					$"Start item expected top alignment within 2 DIP; observed label top={startLabel.Top:F2}, collection top={startCollection.Top:F2}");
				Assert.True(
					Math.Abs(centerLabel.Center - centerCollection.Center) <= Tolerance,
					$"Center item expected center alignment within 2 DIP; observed label center={centerLabel.Center:F2}, collection center={centerCollection.Center:F2}");
				Assert.True(
					Math.Abs(endLabel.Bottom - endCollection.Bottom) <= Tolerance,
					$"End item expected bottom alignment within 2 DIP; observed label bottom={endLabel.Bottom:F2}, collection bottom={endCollection.Bottom:F2}");
			});
		}

		static CalibrationScene CreateCalibrationScene()
		{
			var scene = new CalibrationScene();
			scene.StartLabel = CreateLabel("Start", LayoutOptions.Start);
			scene.CenterLabel = CreateLabel("Center", LayoutOptions.Center);
			scene.EndLabel = CreateLabel("End", LayoutOptions.End);
			TrackNativeLoaded(scene.StartLabel, () => scene.StartLoaded = true);
			TrackNativeLoaded(scene.CenterLabel, () => scene.CenterLoaded = true);
			TrackNativeLoaded(scene.EndLabel, () => scene.EndLoaded = true);

			Grid.SetColumn(scene.CenterLabel, 1);
			Grid.SetColumn(scene.EndLabel, 2);
			scene.Middle = CreateThreeColumnGrid();
			scene.Middle.Add(scene.StartLabel);
			scene.Middle.Add(scene.CenterLabel);
			scene.Middle.Add(scene.EndLabel);
			scene.Page = new ContentPage { Content = CreateThreeRowGrid(scene.Middle) };
			return scene;
		}

		static ReportedScene CreateReportedScene()
		{
			var scene = new ReportedScene();
			scene.StartCollection = CreateCollection(
				"Item VerticalOptions=\"Start\"",
				LayoutOptions.Start,
				label => scene.StartLabel = label,
				() => scene.StartLabelLoaded = true);
			scene.CenterCollection = CreateCollection(
				"Item VerticalOptions=\"Center\"",
				LayoutOptions.Center,
				label => scene.CenterLabel = label,
				() => scene.CenterLabelLoaded = true);
			scene.EndCollection = CreateCollection(
				"Item VerticalOptions=\"End\"",
				LayoutOptions.End,
				label => scene.EndLabel = label,
				() => scene.EndLabelLoaded = true);
			TrackNativeLoaded(scene.StartCollection, () => scene.StartCollectionLoaded = true);
			TrackNativeLoaded(scene.CenterCollection, () => scene.CenterCollectionLoaded = true);
			TrackNativeLoaded(scene.EndCollection, () => scene.EndCollectionLoaded = true);

			Grid.SetColumn(scene.CenterCollection, 1);
			Grid.SetColumn(scene.EndCollection, 2);
			var middle = CreateThreeColumnGrid();
			middle.Add(scene.StartCollection);
			middle.Add(scene.CenterCollection);
			middle.Add(scene.EndCollection);
			scene.Page = new ContentPage { Content = CreateThreeRowGrid(middle) };
			return scene;
		}

		static CollectionView CreateCollection(string text, LayoutOptions verticalOptions, Action<Label> capture, Action markLoaded)
		{
			return new CollectionView
			{
				BackgroundColor = Colors.Transparent,
				SelectionMode = SelectionMode.Single,
				HorizontalOptions = LayoutOptions.Center,
				ItemsSource = new[] { text },
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal) { ItemSpacing = 10 },
				ItemTemplate = new DataTemplate(() =>
				{
					var label = CreateLabel(text, verticalOptions);
					label.SetBinding(Label.TextProperty, ".");
					capture(label);
					TrackNativeLoaded(label, markLoaded);
					return label;
				})
			};
		}

		static Label CreateLabel(string text, LayoutOptions verticalOptions) =>
			new Label
			{
				Text = text,
				BackgroundColor = Colors.Red,
				TextColor = Colors.White,
				HeightRequest = ItemHeight,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalOptions = verticalOptions
			};

		static Grid CreateThreeColumnGrid() =>
			new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				}
			};

		static Grid CreateThreeRowGrid(Grid middle)
		{
			var root = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star)
				}
			};
			var top = new BoxView { BackgroundColor = Colors.Black };
			var bottom = new BoxView { BackgroundColor = Colors.Black };
			Grid.SetRow(middle, 1);
			Grid.SetRow(bottom, 2);
			root.Add(top);
			root.Add(middle);
			root.Add(bottom);
			return root;
		}

		static void TrackNativeLoaded(VisualElement element, Action markLoaded)
		{
			element.HandlerChanged += (_, _) =>
			{
				if (element.Handler?.PlatformView is WFrameworkElement platformView)
					platformView.Loaded += (_, _) => markLoaded();
			};
		}

		static async Task WaitForLoadedAndSized(VisualElement element, Func<bool> loaded)
		{
			await AssertEventually(loaded, timeout: RealizationTimeout);
			await AssertEventually(() =>
			{
				var platformView = (WFrameworkElement)element.ToPlatform();
				return platformView.ActualWidth > 0 && platformView.ActualHeight > 0;
			}, timeout: RealizationTimeout);
		}

		static void AssertItem(Label label, string text, LayoutOptions verticalOptions)
		{
			Assert.Equal(text, label.Text);
			Assert.Equal(verticalOptions, label.VerticalOptions);
			Assert.Equal(Colors.Red, label.BackgroundColor);
			Assert.Equal(Colors.White, label.TextColor);
			Assert.Equal(ItemHeight, label.HeightRequest);
			var nativeLabel = (WFrameworkElement)label.ToPlatform();
			Assert.True(
				Math.Abs(nativeLabel.ActualHeight - ItemHeight) <= Tolerance,
				$"Expected realized item height of 25 DIP; observed {nativeLabel.ActualHeight:F2}");
		}

		static void AssertAlignment(string name, double expected, double actual) =>
			Assert.True(
				Math.Abs(actual - expected) <= Tolerance,
				$"{name} oracle expected alignment within 2 DIP; observed target={actual:F2}, container={expected:F2}");

		static (double Top, double Center, double Bottom) GetNativeBounds(VisualElement element)
		{
			var platformView = (WFrameworkElement)element.ToPlatform();
			var top = platformView.GetLocationOnScreen().Value.Y;
			return (top, top + platformView.ActualHeight / 2, top + platformView.ActualHeight);
		}

		sealed class CalibrationScene
		{
			public ContentPage Page;
			public Grid Middle;
			public Label StartLabel;
			public Label CenterLabel;
			public Label EndLabel;
			public bool StartLoaded;
			public bool CenterLoaded;
			public bool EndLoaded;
		}

		sealed class ReportedScene
		{
			public ContentPage Page;
			public CollectionView StartCollection;
			public CollectionView CenterCollection;
			public CollectionView EndCollection;
			public Label StartLabel;
			public Label CenterLabel;
			public Label EndLabel;
			public bool StartCollectionLoaded;
			public bool CenterCollectionLoaded;
			public bool EndCollectionLoaded;
			public bool StartLabelLoaded;
			public bool CenterLabelLoaded;
			public bool EndLabelLoaded;
		}
	}
}

