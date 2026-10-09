#if WINDOWS
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WBorder = Microsoft.UI.Xaml.Controls.Border;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WButtonAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer;
using WControl = Microsoft.UI.Xaml.Controls.Control;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WIInvokeProvider = Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider;
using WPanel = Microsoft.UI.Xaml.Controls.Panel;
using WPatternInterface = Microsoft.UI.Xaml.Automation.Peers.PatternInterface;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue25407")]
	public class Issue25407 : ControlsHandlerTestBase
	{
		const string ExpectedFailureSignature = "Issue25407 affected native label background did not update after the nested LabelStyle binding";

		[Fact]
		[RequiresUnreferencedCode("Runtime XAML loading may require unreferenced code")]
		public async Task NestedBindableObjectBindingUpdatesNativeLabelBackground()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Issue25407BindableLabelControl, ContentViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var page = new ContentPage();
			page.LoadFromXaml(
				"""
				<ContentPage
					xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
					xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
					xmlns:local="clr-namespace:Microsoft.Maui.DeviceTests;assembly=Microsoft.Maui.Controls.DeviceTests">
					<ScrollView x:Name="IssueScrollView">
						<VerticalStackLayout x:Name="IssueLayout" Padding="24" Spacing="16">
							<local:Issue25407BindableLabelControl
								x:Name="AffectedControl"
								AutomationId="AffectedControl"
								HeightRequest="120">
								<local:Issue25407BindableLabelControl.Content>
									<Label
										AutomationId="AffectedLabel"
										FontSize="24"
										HorizontalTextAlignment="Center"
										HorizontalOptions="Fill"
										VerticalOptions="Fill"
										VerticalTextAlignment="Center"
										Text="Bound label: expected violet background" />
								</local:Issue25407BindableLabelControl.Content>
								<local:Issue25407BindableLabelControl.LabelStyle>
									<local:Issue25407LabelStyle BackgroundColor="{Binding LabelBackgroundColor}" />
								</local:Issue25407BindableLabelControl.LabelStyle>
							</local:Issue25407BindableLabelControl>
							<Button
								x:Name="ApplyBindingButton"
								AutomationId="ApplyBindingButton"
								Text="Apply violet binding" />
						</VerticalStackLayout>
					</ScrollView>
				</ContentPage>
				""");

			var scrollView = page.FindByName<ScrollView>("IssueScrollView");
			var layout = page.FindByName<VerticalStackLayout>("IssueLayout");
			var affectedControl = page.FindByName<Issue25407BindableLabelControl>("AffectedControl");
			var applyButton = page.FindByName<Button>("ApplyBindingButton");
			Assert.NotNull(scrollView);
			Assert.NotNull(layout);
			Assert.NotNull(affectedControl);
			Assert.NotNull(applyButton);
			Assert.NotNull(affectedControl.LabelStyle);

			var affectedLabel = affectedControl.StyledLabel;
			var expectedViewModel = new Issue25407ViewModel();
			var expectedColor = expectedViewModel.LabelBackgroundColor;
			var companionControl = new Issue25407BindableLabelControl
			{
				HeightRequest = 120,
				Content = new Label
				{
					AutomationId = "CompanionLabel",
					FontSize = 24,
					HorizontalTextAlignment = TextAlignment.Center,
					HorizontalOptions = LayoutOptions.Fill,
					VerticalOptions = LayoutOptions.Fill,
					VerticalTextAlignment = TextAlignment.Center,
					Text = "Direct label: violet background"
				},
				LabelStyle = new Issue25407LabelStyle
				{
					BackgroundColor = expectedColor
				}
			};
			layout.Children.Add(companionControl);

			Assert.Null(page.BindingContext);
			Assert.True(affectedControl.LabelStyle.IsSet(Issue25407LabelStyle.BackgroundColorProperty));
			Assert.Equal(Colors.Red, affectedControl.LabelStyle.BackgroundColor);
			Assert.Equal("AffectedControl", affectedControl.AutomationId);
			Assert.Equal("AffectedLabel", affectedLabel.AutomationId);
			Assert.Equal("Bound label: expected violet background", affectedLabel.Text);
			Assert.Equal(120d, affectedControl.HeightRequest);

			var clicked = false;
			var bindingContextChanged = false;
			var dispatchCompleted = false;

			page.BindingContextChanged += (_, _) => bindingContextChanged = true;
			applyButton.Clicked += (_, _) =>
			{
				clicked = true;
				page.BindingContext = expectedViewModel;
				page.Dispatcher.Dispatch(() => dispatchCompleted = true);
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(page.Window);
				Assert.NotNull(page.Window.Handler);
				Assert.NotNull(page.Window.Handler.PlatformView);

				var nativePage = GetNativeElement(page);
				var nativeScrollView = GetNativeElement(scrollView);
				var nativeLayout = GetNativeElement(layout);
				var nativeAffectedControl = GetNativeElement(affectedControl);
				var nativeAffectedLabel = GetNativeElement(affectedLabel);
				var nativeApplyButton = GetNativeElement(applyButton);
				var nativeCompanionLabel = GetNativeElement(companionControl.StyledLabel);

				Assert.True(nativePage.IsLoaded);
				Assert.True(nativeScrollView.IsLoaded);
				Assert.True(nativeLayout.IsLoaded);
				Assert.True(nativeAffectedControl.IsLoaded);
				Assert.True(nativeAffectedLabel.IsLoaded);
				Assert.True(nativeApplyButton.IsLoaded);
				Assert.True(nativeCompanionLabel.IsLoaded);
				Assert.True(nativePage.ActualWidth > 0);
				Assert.True(nativePage.ActualHeight > 0);
				Assert.True(nativeScrollView.ActualWidth > 0);
				Assert.True(nativeScrollView.ActualHeight > 0);
				Assert.True(nativeLayout.ActualWidth > 0);
				Assert.True(nativeLayout.ActualHeight > 0);
				Assert.True(nativeAffectedControl.ActualWidth > 0);
				Assert.True(Math.Abs(nativeAffectedControl.ActualHeight - 120) < 1);
				Assert.True(nativeAffectedLabel.ActualWidth > 0);
				Assert.True(nativeAffectedLabel.ActualHeight > 0);
				Assert.True(nativeApplyButton.ActualWidth > 0);
				Assert.True(nativeApplyButton.ActualHeight > 0);
				Assert.True(nativeCompanionLabel.ActualWidth > 0);
				Assert.True(nativeCompanionLabel.ActualHeight > 0);

				Assert.Equal(expectedColor, companionControl.LabelStyle.BackgroundColor);
				AssertColorClose(expectedColor, GetNativeBackground(nativeCompanionLabel),
					"Issue25407 companion native label did not render its directly assigned violet background");
				AssertColorClose(Colors.Red, GetNativeBackground(nativeAffectedLabel),
					"Issue25407 affected native label did not render the initial red default");

				Assert.False(clicked);
				Assert.False(bindingContextChanged);
				Assert.False(dispatchCompleted);

				var nativeButton = Assert.IsAssignableFrom<WButton>(applyButton.Handler.PlatformView);
				var automationPeer = new WButtonAutomationPeer(nativeButton);
				var invokeProvider = Assert.IsAssignableFrom<WIInvokeProvider>(
					automationPeer.GetPattern(WPatternInterface.Invoke));
				invokeProvider.Invoke();

				await AssertHelpers.AssertEventually(() => clicked, message: "Issue25407 native button invocation did not raise Clicked");
				await AssertHelpers.AssertEventually(() => bindingContextChanged, message: "Issue25407 page BindingContext did not change");
				await AssertHelpers.AssertEventually(() => dispatchCompleted, message: "Issue25407 post-binding dispatcher callback did not complete");

				Assert.Same(expectedViewModel, page.BindingContext);

				Color observedColor = GetNativeBackground(nativeAffectedLabel);
				var updated = await AssertHelpers.Wait(() =>
				{
					observedColor = GetNativeBackground(nativeAffectedLabel);
					return ColorsAreClose(expectedColor, observedColor);
				});

				Assert.True(updated,
					$"{ExpectedFailureSignature}. Observed {observedColor}, expected {expectedColor}.");
			});
		}

		static WFrameworkElement GetNativeElement(Element element)
		{
			Assert.NotNull(element.Handler);
			Assert.NotNull(element.Handler.PlatformView);
			return element.Handler.ToPlatform();
		}

		static Color GetNativeBackground(WFrameworkElement nativeElement)
		{
			var nativeBrush = nativeElement switch
			{
				WControl control => control.Background as WSolidColorBrush,
				WBorder border => border.Background as WSolidColorBrush,
				WPanel panel => panel.Background as WSolidColorBrush,
				_ => null
			};

			Assert.NotNull(nativeBrush);
			return nativeBrush.Color.ToColor();
		}

		static void AssertColorClose(Color expected, Color observed, string message) =>
			Assert.True(ColorsAreClose(expected, observed), $"{message}. Observed {observed}, expected {expected}.");

		static bool ColorsAreClose(Color expected, Color observed)
		{
			const float tolerance = 0.01f;
			return Math.Abs(expected.Red - observed.Red) <= tolerance
				&& Math.Abs(expected.Green - observed.Green) <= tolerance
				&& Math.Abs(expected.Blue - observed.Blue) <= tolerance
				&& Math.Abs(expected.Alpha - observed.Alpha) <= tolerance;
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

	public sealed class Issue25407BindableLabelControl : ContentView
	{
		public static readonly BindableProperty LabelStyleProperty = BindableProperty.Create(
			nameof(LabelStyle),
			typeof(Issue25407LabelStyle),
			typeof(Issue25407BindableLabelControl),
			default(Issue25407LabelStyle),
			propertyChanged: OnLabelStyleChanged);

		public Label StyledLabel => (Label)Content;

		public Issue25407LabelStyle LabelStyle
		{
			get => (Issue25407LabelStyle)GetValue(LabelStyleProperty);
			set => SetValue(LabelStyleProperty, value);
		}

		static void OnLabelStyleChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var control = (Issue25407BindableLabelControl)bindable;

			if (oldValue is Issue25407LabelStyle oldStyle)
				oldStyle.PropertyChanged -= control.OnLabelStylePropertyChanged;

			if (newValue is Issue25407LabelStyle newStyle)
				newStyle.PropertyChanged += control.OnLabelStylePropertyChanged;

			control.ApplyLabelStyle();
		}

		void OnLabelStylePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Issue25407LabelStyle.BackgroundColorProperty.PropertyName)
				ApplyLabelStyle();
		}

		void ApplyLabelStyle()
		{
			StyledLabel.BackgroundColor = LabelStyle?.BackgroundColor ?? Colors.Red;
		}
	}
}
#endif

