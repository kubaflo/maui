#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WRadioButton = Microsoft.UI.Xaml.Controls.RadioButton;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30752")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue30752 : ControlsHandlerTestBase
	{
		const double TemplateChromeWidth = 48;
		const double WidthTolerance = 2;

		[Fact]
		public async Task TemplatedRadioButtonSizesToItsContent()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<HorizontalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Ellipse, ShapeViewHandler>();
					handlers.AddHandler<ContentPresenter, ContentViewHandler>();
					handlers.AddHandler<RadioButton, RadioButtonHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var template = CreateRadioButtonTemplate();
			var clientLabel = new Label { Text = "Client ID", FontSize = 24 };
			var taxLabel = new Label { Text = "Tax Number", FontSize = 24 };
			var billLabel = new Label { Text = "Bill", FontSize = 24 };
			var clientRadio = new RadioButton
			{
				AutomationId = "ClientRadio",
				ControlTemplate = template,
				IsChecked = true,
				Content = clientLabel
			};
			var taxRadio = new RadioButton
			{
				AutomationId = "TaxRadio",
				ControlTemplate = template,
				Content = taxLabel
			};
			var billRadio = new RadioButton
			{
				AutomationId = "BillRadio",
				ControlTemplate = template,
				Content = billLabel
			};
			var optionsRow = new HorizontalStackLayout
			{
				AutomationId = "OptionsRow",
				Spacing = 0,
				Children =
				{
					clientRadio,
					taxRadio,
					billRadio
				}
			};
			optionsRow.SetValue(RadioButtonGroup.GroupNameProperty, "SearchMode");

			var resultLabel = new Label
			{
				AutomationId = "ResultLabel",
				Text = "Radio width measurements",
				FontAttributes = FontAttributes.Bold
			};
			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 14,
					Children =
					{
						new Label { Text = "Search?", FontSize = 28 },
						optionsRow,
						new Entry { Placeholder = "Client ID", FontSize = 24 },
						new Button
						{
							AutomationId = "CheckWidthButton",
							Text = "Check radio widths"
						},
						resultLabel
					}
				}
			};

			double clientSizeChangedWidth = -1;
			double taxSizeChangedWidth = -1;
			double billSizeChangedWidth = -1;
			clientRadio.SizeChanged += (_, _) => clientSizeChangedWidth = clientRadio.Width;
			taxRadio.SizeChanged += (_, _) => taxSizeChangedWidth = taxRadio.Width;
			billRadio.SizeChanged += (_, _) => billSizeChangedWidth = billRadio.Width;

			await AttachAndRun(page, async _ =>
			{
				await AssertEventually(() =>
					clientSizeChangedWidth > 0 &&
					taxSizeChangedWidth > 0 &&
					billSizeChangedWidth > 0);

				Assert.Same(clientLabel, clientRadio.Content);
				Assert.Same(taxLabel, taxRadio.Content);
				Assert.Same(billLabel, billRadio.Content);
				Assert.Equal("Client ID", clientLabel.Text);
				Assert.Equal("Tax Number", taxLabel.Text);
				Assert.Equal("Bill", billLabel.Text);

				Assert.NotNull(clientRadio.Handler);
				Assert.NotNull(taxRadio.Handler);
				Assert.NotNull(billRadio.Handler);
				Assert.NotNull(clientLabel.Handler);
				Assert.NotNull(taxLabel.Handler);
				Assert.NotNull(billLabel.Handler);

				var nativeClient = Assert.IsType<WRadioButton>(
					((RadioButtonHandler)clientRadio.Handler).PlatformView);
				var nativeTax = Assert.IsType<WRadioButton>(
					((RadioButtonHandler)taxRadio.Handler).PlatformView);
				var nativeBill = Assert.IsType<WRadioButton>(
					((RadioButtonHandler)billRadio.Handler).PlatformView);
				var nativeClientLabel = ((LabelHandler)clientLabel.Handler).PlatformView;
				var nativeTaxLabel = ((LabelHandler)taxLabel.Handler).PlatformView;
				var nativeBillLabel = ((LabelHandler)billLabel.Handler).PlatformView;

				Assert.True(nativeClient.IsLoaded);
				Assert.True(nativeTax.IsLoaded);
				Assert.True(nativeBill.IsLoaded);
				Assert.True(nativeClient.ActualWidth > 0);
				Assert.True(nativeTax.ActualWidth > 0);
				Assert.True(nativeBill.ActualWidth > 0);
				Assert.True(nativeClientLabel.ActualWidth > 0);
				Assert.True(nativeTaxLabel.ActualWidth > 0);
				Assert.True(nativeBillLabel.ActualWidth > 0);
				Assert.True(page.Width > nativeClient.ActualWidth + nativeTax.ActualWidth + nativeBill.ActualWidth);

				AssertWidthMatchesContent("Client ID", nativeClient.ActualWidth, nativeClientLabel.ActualWidth);
				AssertWidthMatchesContent("Tax Number", nativeTax.ActualWidth, nativeTaxLabel.ActualWidth);
				AssertWidthMatchesContent("Bill", nativeBill.ActualWidth, nativeBillLabel.ActualWidth);
			});
		}

		static ControlTemplate CreateRadioButtonTemplate()
		{
			return new ControlTemplate(() =>
			{
				var checkedIndicator = new Ellipse
				{
					WidthRequest = 14,
					HeightRequest = 14,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					Fill = Color.FromArgb("#27749B")
				};
				var checkedState = new VisualState { Name = "Checked" };
				checkedState.Setters.Add(new Setter
				{
					TargetName = "CheckedIndicator",
					Property = VisualElement.OpacityProperty,
					Value = 1d
				});
				var uncheckedState = new VisualState { Name = "Unchecked" };
				uncheckedState.Setters.Add(new Setter
				{
					TargetName = "CheckedIndicator",
					Property = VisualElement.OpacityProperty,
					Value = 0d
				});
				var checkedStates = new VisualStateGroup { Name = "CheckedStates" };
				checkedStates.States.Add(checkedState);
				checkedStates.States.Add(uncheckedState);

				var root = new Grid
				{
					Padding = new Thickness(4, 0),
					ColumnDefinitions =
					{
						new ColumnDefinition { Width = 32 },
						new ColumnDefinition { Width = GridLength.Auto }
					},
					ColumnSpacing = 8
				};
				INameScope nameScope = new NameScope();
				NameScope.SetNameScope(root, nameScope);
				nameScope.RegisterName("CheckedIndicator", checkedIndicator);
				VisualStateManager.SetVisualStateGroups(
					root,
					new VisualStateGroupList { checkedStates });
				root.Add(new Ellipse
				{
					WidthRequest = 28,
					HeightRequest = 28,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					Stroke = Color.FromArgb("#27749B"),
					StrokeThickness = 2
				});
				root.Add(checkedIndicator);
				root.Add(new ContentPresenter
				{
					VerticalOptions = LayoutOptions.Center
				}, 1);
				return root;
			});
		}

		static void AssertWidthMatchesContent(string name, double actualWidth, double labelWidth)
		{
			double expectedWidth = labelWidth + TemplateChromeWidth;
			Assert.True(
				Math.Abs(actualWidth - expectedWidth) <= WidthTolerance,
				$"Templated {name} RadioButton width should equal its rendered content plus the 48 DIP template chrome. " +
				$"Actual: {actualWidth:F2}, expected: {expectedWidth:F2}, label: {labelWidth:F2}, " +
				$"chrome: {TemplateChromeWidth:F2}, tolerance: {WidthTolerance:F2}.");
		}
	}
}
#endif

