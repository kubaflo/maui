#if WINDOWS
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WPanel = Microsoft.UI.Xaml.Controls.Panel;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue25407")]
	public class Issue25407 : ControlsHandlerTestBase
	{
		const string ExpectedFailureSignature = "Issue 25407 affected native Label background should match the bound LabelBackgroundColor";

		[Fact]
		[RequiresUnreferencedCode("Runtime XAML loading requires unreferenced code")]
		public async Task BindableObjectMemberUsesInheritedBindingContext()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
				});
			});

			var viewModel = new Issue25407ViewModel();
			var plainLabel = new Label
			{
				Text = "Oracle label",
				BackgroundColor = viewModel.LabelBackgroundColor
			};

			await AttachAndRun<LabelHandler>(plainLabel, async handler =>
			{
				await OnLoadedAsync(plainLabel);
				var oraclePanel = Assert.IsAssignableFrom<WPanel>(handler.ContainerView);
				var oracleBrush = Assert.IsType<WSolidColorBrush>(oraclePanel.Background);
				AssertColorMatches(viewModel.LabelBackgroundColor, oracleBrush, "Plain Label native color oracle was invalid");
			});

			var page = new ContentPage();
			page.LoadFromXaml(
				"""
				<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
					xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
					xmlns:local="clr-namespace:Microsoft.Maui.DeviceTests;assembly=Microsoft.Maui.Controls.DeviceTests">
					<Grid Padding="24"
						RowDefinitions="Auto,Auto,Auto,Auto"
						RowSpacing="16">
						<local:Issue25407LabelView
							x:Name="AffectedControl"
							Grid.Row="1">
							<local:Issue25407LabelView.LabelStyle>
								<local:Issue25407LabelStyle BackgroundColor="{Binding LabelBackgroundColor}" />
							</local:Issue25407LabelView.LabelStyle>
						</local:Issue25407LabelView>
					</Grid>
				</ContentPage>
				""");
			page.BindingContext = viewModel;

			var grid = Assert.IsType<Grid>(page.Content);
			var affectedControl = Assert.IsType<Issue25407LabelView>(Assert.Single(grid.Children));
			var affectedLabel = affectedControl.AffectedLabel;
			bool loadedCallbackObserved = false;
			affectedLabel.Loaded += (_, _) => loadedCallbackObserved = true;

			await AttachAndRun(page, async _ =>
			{
				await OnLoadedAsync(affectedLabel);

				Assert.True(loadedCallbackObserved);
				Assert.Equal("Affected label", affectedLabel.Text);
				Assert.NotNull(affectedControl.LabelStyle);
				Assert.True(affectedControl.LabelStyle.IsSet(Issue25407LabelStyle.BackgroundColorProperty));
				Assert.NotNull(affectedLabel.Handler);
				var labelHandler = Assert.IsType<LabelHandler>(affectedLabel.Handler);
				Assert.NotNull(labelHandler.PlatformView);

				await AssertEventually(
					() => labelHandler.PlatformView.ActualWidth > 0 &&
						labelHandler.PlatformView.ActualHeight > 0 &&
						labelHandler.ContainerView is WPanel { Background: WSolidColorBrush });

				var affectedPanel = Assert.IsAssignableFrom<WPanel>(labelHandler.ContainerView);
				var affectedBrush = Assert.IsType<WSolidColorBrush>(affectedPanel.Background);
				AssertColorMatches(viewModel.LabelBackgroundColor, affectedBrush, ExpectedFailureSignature);
			});
		}

		static void AssertColorMatches(Color expected, WSolidColorBrush actualBrush, string message)
		{
			var actual = actualBrush.Color;
			byte expectedAlpha = (byte)Math.Round(expected.Alpha * byte.MaxValue);
			byte expectedRed = (byte)Math.Round(expected.Red * byte.MaxValue);
			byte expectedGreen = (byte)Math.Round(expected.Green * byte.MaxValue);
			byte expectedBlue = (byte)Math.Round(expected.Blue * byte.MaxValue);

			bool matches = Math.Abs(actual.A - expectedAlpha) <= 1 &&
				Math.Abs(actual.R - expectedRed) <= 1 &&
				Math.Abs(actual.G - expectedGreen) <= 1 &&
				Math.Abs(actual.B - expectedBlue) <= 1;

			Assert.True(
				matches,
				$"{message}. Measured ARGB={actual.A:X2}{actual.R:X2}{actual.G:X2}{actual.B:X2}; expected ARGB={expectedAlpha:X2}{expectedRed:X2}{expectedGreen:X2}{expectedBlue:X2}.");
		}
	}

	public sealed class Issue25407ViewModel
	{
		public Color LabelBackgroundColor => Colors.Violet;
	}

	public sealed class Issue25407LabelStyle : BindableObject
	{
		public static readonly BindableProperty BackgroundColorProperty = BindableProperty.Create(
			nameof(BackgroundColor),
			typeof(Color),
			typeof(Issue25407LabelStyle),
			Colors.Red);

		public Color BackgroundColor
		{
			get => (Color)GetValue(BackgroundColorProperty);
			set => SetValue(BackgroundColorProperty, value);
		}
	}

	public sealed class Issue25407LabelView : ContentView
	{
		public static readonly BindableProperty LabelStyleProperty = BindableProperty.Create(
			nameof(LabelStyle),
			typeof(Issue25407LabelStyle),
			typeof(Issue25407LabelView),
			propertyChanged: OnLabelStyleChanged);

		public Label AffectedLabel
		{
			get
			{
				if (Content is Label label)
					return label;

				label = new Label
				{
					Text = "Affected label"
				};
				Content = label;
				return label;
			}
		}

		public Issue25407LabelStyle LabelStyle
		{
			get => (Issue25407LabelStyle)GetValue(LabelStyleProperty);
			set => SetValue(LabelStyleProperty, value);
		}

		static void OnLabelStyleChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var control = (Issue25407LabelView)bindable;
			if (newValue is Issue25407LabelStyle labelStyle)
			{
				control.AffectedLabel.SetBinding(
					Label.BackgroundColorProperty,
					new Binding(nameof(Issue25407LabelStyle.BackgroundColor), source: labelStyle));
			}
		}
	}
}
#endif

