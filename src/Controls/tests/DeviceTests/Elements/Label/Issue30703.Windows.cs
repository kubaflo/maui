#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30703")]
	public class Issue30703 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RotatedLabelInFixedGridColumnRetainsIntrinsicTextWidth()
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
					handlers.AddHandler<BoxView, BoxViewHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			const string labelText = "This as a long text";
			const double tolerance = 1;

			var healthyBackground = new BoxView { Color = Colors.GreenYellow };
			Grid.SetRowSpan(healthyBackground, 2);

			var affectedBackground = new BoxView { Color = Colors.Aqua };
			Grid.SetColumn(affectedBackground, 1);
			Grid.SetRowSpan(affectedBackground, 2);

			var thirdBackground = new BoxView { Color = Colors.Beige };
			Grid.SetColumn(thirdBackground, 2);
			Grid.SetRowSpan(thirdBackground, 2);

			var healthyRotatedLabel = CreateScenarioLabel(labelText, 90);
			healthyRotatedLabel.AutomationId = "HealthyRotatedLabel";

			var affectedRotatedLabel = CreateScenarioLabel(labelText, 90);
			affectedRotatedLabel.AutomationId = "AffectedRotatedLabel";
			Grid.SetColumn(affectedRotatedLabel, 1);
			Assert.False(affectedRotatedLabel.IsSet(VisualElement.StyleProperty));

			var thirdRotatedLabel = CreateScenarioLabel(labelText, 90);
			Grid.SetColumn(thirdRotatedLabel, 2);

			var healthyUnrotatedLabel = CreateScenarioLabel(labelText, 0);
			Grid.SetRow(healthyUnrotatedLabel, 1);

			var affectedUnrotatedLabel = CreateScenarioLabel(labelText, 0);
			Grid.SetRow(affectedUnrotatedLabel, 1);
			Grid.SetColumn(affectedUnrotatedLabel, 1);

			var thirdUnrotatedLabel = CreateScenarioLabel(labelText, 0);
			Grid.SetRow(thirdUnrotatedLabel, 1);
			Grid.SetColumn(thirdUnrotatedLabel, 2);

			var instructionLabel = new Label
			{
				AutomationId = "RotationInstruction",
				Text = "Inspect the rotated labels",
				BackgroundColor = Colors.White,
				Padding = 8,
			};

			var checkButton = new Button
			{
				AutomationId = "CheckRotation",
				Text = "Check rotated label",
			};

			var statusStack = new VerticalStackLayout
			{
				Margin = 12,
				Spacing = 8,
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Start,
				Children =
				{
					instructionLabel,
					checkButton,
				},
			};

			var scenarioGrid = new Grid
			{
				AutomationId = "RotationScenario",
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Star },
				},
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = 40 },
					new ColumnDefinition { Width = 80 },
				},
				Children =
				{
					healthyBackground,
					affectedBackground,
					thirdBackground,
					healthyRotatedLabel,
					affectedRotatedLabel,
					thirdRotatedLabel,
					healthyUnrotatedLabel,
					affectedUnrotatedLabel,
					thirdUnrotatedLabel,
					statusStack,
				},
			};

			var page = new ContentPage
			{
				Title = "Home",
				Content = scenarioGrid,
			};

			LabelHandler affectedHandler = null;
			WTextBlock affectedTextBlock = null;
			double observedNativeWidth = -1;
			affectedRotatedLabel.HandlerChanged += OnAffectedHandlerChanged;

			try
			{
				await CreateHandlerAndAddToWindow<PageHandler>(page, async _ =>
				{
					Assert.NotNull(affectedHandler);
					Assert.NotNull(affectedTextBlock);

					await AssertEventually(
						() => observedNativeWidth > 0,
						timeout: 2000,
						message: "The affected Windows TextBlock did not complete native layout.");

					var healthyHandler = Assert.IsType<LabelHandler>(healthyRotatedLabel.Handler);
					var healthyTextBlock = Assert.IsType<WTextBlock>(healthyHandler.PlatformView);
					Assert.Same(affectedTextBlock, affectedHandler.PlatformView);
					Assert.Equal(labelText, affectedTextBlock.Text);
					Assert.Equal(90, affectedRotatedLabel.Rotation);
					Assert.Equal(1, Grid.GetColumn(affectedRotatedLabel));
					Assert.Same(affectedRotatedLabel, scenarioGrid.Children[4]);
					Assert.True(affectedTextBlock.ActualWidth > 0);
					Assert.True(affectedTextBlock.ActualHeight > 0);

					Assert.Equal(Colors.GreenYellow, healthyBackground.Color);
					Assert.Equal(Colors.Aqua, affectedBackground.Color);
					Assert.Equal(Colors.Beige, thirdBackground.Color);
					Assert.Equal(Colors.Orange, healthyRotatedLabel.BackgroundColor);
					Assert.Equal(Colors.Orange, affectedRotatedLabel.BackgroundColor);
					Assert.Equal(LayoutOptions.Center, affectedRotatedLabel.HorizontalOptions);
					Assert.Equal(LayoutOptions.Center, affectedRotatedLabel.VerticalOptions);

					var nativeGrid = Assert.IsAssignableFrom<WFrameworkElement>(scenarioGrid.Handler.PlatformView);
					var rowHeight = nativeGrid.ActualHeight / 2;
					var requiredIntrinsicWidth = healthyTextBlock.ActualWidth;

					Assert.True(
						rowHeight + tolerance >= requiredIntrinsicWidth,
						$"The test row height {rowHeight:F2} cannot contain the rotated intrinsic width {requiredIntrinsicWidth:F2}.");
					Assert.True(
						requiredIntrinsicWidth > scenarioGrid.ColumnDefinitions[2].Width.Value,
						$"The healthy rotated label width {requiredIntrinsicWidth:F2} did not exceed the widest fixed test column.");
					Assert.True(
						affectedTextBlock.ActualWidth + tolerance >= requiredIntrinsicWidth,
						$"Issue30703 rotated label is clipped on Windows: actual width {affectedTextBlock.ActualWidth:F2}, required intrinsic width {requiredIntrinsicWidth:F2}, row height {rowHeight:F2}, tolerance {tolerance:F2}.");
				});
			}
			finally
			{
				affectedRotatedLabel.HandlerChanged -= OnAffectedHandlerChanged;
				if (affectedTextBlock is not null)
					affectedTextBlock.LayoutUpdated -= OnNativeLayoutUpdated;
			}

			void OnAffectedHandlerChanged(object sender, EventArgs args)
			{
				affectedHandler = affectedRotatedLabel.Handler as LabelHandler
					?? throw new InvalidOperationException("The affected Label did not receive the standard LabelHandler.");

				affectedTextBlock = affectedHandler.PlatformView
					?? throw new InvalidOperationException("The affected LabelHandler did not create a Windows TextBlock.");
				affectedTextBlock.LayoutUpdated += OnNativeLayoutUpdated;
			}

			void OnNativeLayoutUpdated(object sender, object args)
			{
				observedNativeWidth = affectedTextBlock.ActualWidth;
			}
		}

		static Label CreateScenarioLabel(string text, double rotation)
		{
			return new Label
			{
				Text = text,
				BackgroundColor = Colors.Orange,
				Rotation = rotation,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
			};
		}
	}
}
#endif

