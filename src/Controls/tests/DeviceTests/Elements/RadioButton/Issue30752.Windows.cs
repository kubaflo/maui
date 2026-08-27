using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WRadioButton = Microsoft.UI.Xaml.Controls.RadioButton;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30752")]
	public class Issue30752 : ControlsHandlerTestBase
	{
#if WINDOWS
		const double WidthTolerance = 1;

		[Fact]
		public async Task CustomTemplatedRadioButtonsSizeToTheirContent()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<HorizontalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<ContentPresenter, ContentViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<RadioButton, RadioButtonHandler>();
				});
			});

			string[] content = ["Client ID", "Tax Number", "Bill"];
			var expectedWidths = new double[content.Length];

			await InvokeOnMainThreadAsync(async () =>
			{
				var cleanLoaded = new TaskCompletionSource<bool>();
				var cleanLabels = content.Select(text => new Label { Text = text, FontSize = 14 }).ToArray();
				var cleanBorders = cleanLabels.Select(CreateTemplateBody).ToArray();
				var cleanRow = new HorizontalStackLayout { Spacing = 0 };
				foreach (var border in cleanBorders)
					cleanRow.Add(border);

				cleanRow.Loaded += (_, _) => cleanLoaded.TrySetResult(true);

				await AttachAndRun(cleanRow, async _ =>
				{
					Assert.True(await cleanLoaded.Task.WaitAsync(TimeSpan.FromSeconds(5)), "Clean template bodies did not load.");

					var nativeBorders = cleanBorders
						.Select(border => Assert.IsType<BorderHandler>(border.Handler).PlatformView)
						.ToArray();

					await AssertEventually(
						() => nativeBorders.All(border => border.ActualWidth > 0),
						timeout: 2000,
						message: "Clean template bodies did not reach positive settled widths.");

					for (int i = 0; i < content.Length; i++)
						expectedWidths[i] = nativeBorders[i].ActualWidth;

					for (int i = 0; i < expectedWidths.Length; i++)
					{
						for (int j = i + 1; j < expectedWidths.Length; j++)
						{
							Assert.True(
								Math.Abs(expectedWidths[i] - expectedWidths[j]) > WidthTolerance,
								$"Clean template bodies for '{content[i]}' and '{content[j]}' must produce distinct content-derived widths.");
						}
					}
				});

				var template = new ControlTemplate(CreateRadioTemplate);
				var radioButtons = new[]
				{
					CreateRadioButton(content[0], template),
					CreateRadioButton(content[1], template),
					CreateRadioButton(content[2], template),
				};
				radioButtons[0].IsChecked = true;
				var radioRow = new HorizontalStackLayout { Spacing = 0 };
				foreach (var radioButton in radioButtons)
					radioRow.Add(radioButton);

				var root = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					VerticalOptions = LayoutOptions.Start,
					Children =
					{
						new Label { Text = "Search?", FontSize = 22 },
						radioRow,
					}
				};
				var page = new ContentPage { Content = root };
				var reportedLoaded = new TaskCompletionSource<bool>();
				page.Loaded += (_, _) => reportedLoaded.TrySetResult(true);

				await AttachAndRun(page, async _ =>
				{
					Assert.True(await reportedLoaded.Task.WaitAsync(TimeSpan.FromSeconds(5)), "Reported RadioButton hierarchy did not load.");
					Assert.True(page.Width > expectedWidths.Sum() + 48, "The test window must not constrain the RadioButton row.");

					var nativeRadioButtons = new WRadioButton[radioButtons.Length];
					for (int i = 0; i < radioButtons.Length; i++)
					{
						Assert.Same(radioButtons[i], radioRow.Children[i]);
						Assert.Equal(content[i], radioButtons[i].Content);
						Assert.Equal("SearchType", radioButtons[i].GroupName);
						Assert.Equal(i == 0, radioButtons[i].IsChecked);
						Assert.Equal(-1, radioButtons[i].WidthRequest);
						Assert.Equal(-1, radioButtons[i].MinimumWidthRequest);

						var handler = Assert.IsType<RadioButtonHandler>(radioButtons[i].Handler);
						nativeRadioButtons[i] = Assert.IsType<WRadioButton>(handler.PlatformView);
					}

					await AssertEventually(
						() => nativeRadioButtons.All(radio => radio.ActualWidth > 0 && radio.ActualHeight > 0),
						timeout: 2000,
						message: "Custom-templated RadioButtons did not reach positive settled native frames.");

					var measurements = string.Join(
						", ",
						content.Select((text, i) => $"{text}: measured={nativeRadioButtons[i].ActualWidth:F1}, expected={expectedWidths[i]:F1}"));
					Assert.True(
						nativeRadioButtons.Select((radio, i) => Math.Abs(radio.ActualWidth - expectedWidths[i])).All(delta => delta <= WidthTolerance),
						$"Custom-templated RadioButton native widths did not match content-derived widths: {measurements}");
				});
			});
		}

		static RadioButton CreateRadioButton(string content, ControlTemplate template) =>
			new RadioButton
			{
				Content = content,
				FontSize = 14,
				GroupName = "SearchType",
				ControlTemplate = template,
			};

		static Border CreateTemplateBody(View textContent)
		{
			var indicator = new Border
			{
				WidthRequest = 18,
				HeightRequest = 18,
				Stroke = Color.FromArgb("#397A9E"),
				StrokeThickness = 2,
				StrokeShape = new RoundRectangle { CornerRadius = 9 },
				VerticalOptions = LayoutOptions.Center,
				Content = new Border
				{
					WidthRequest = 10,
					HeightRequest = 10,
					BackgroundColor = Color.FromArgb("#397A9E"),
					StrokeThickness = 0,
					StrokeShape = new RoundRectangle { CornerRadius = 5 },
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					Opacity = 0,
				},
			};
			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = 20 },
					new ColumnDefinition { Width = GridLength.Auto },
				},
				ColumnSpacing = 4,
			};
			grid.Add(indicator);
			grid.Add(textContent, 1);

			return new Border
			{
				Padding = new Thickness(4, 2),
				BackgroundColor = Color.FromArgb("#E8E8E8"),
				Stroke = Color.FromArgb("#707070"),
				StrokeThickness = 1,
				Content = grid,
			};
		}

		static View CreateRadioTemplate()
		{
			var presenter = new ContentPresenter { VerticalOptions = LayoutOptions.Center };
			var root = CreateTemplateBody(presenter);
			var indicator = Assert.IsType<Border>(Assert.IsType<Grid>(root.Content).Children[0]);
			var checkedIndicator = Assert.IsType<Border>(indicator.Content);
			checkedIndicator.Opacity = 0;

			INameScope nameScope = new NameScope();
			NameScope.SetNameScope(root, nameScope);
			nameScope.RegisterName("CheckedIndicator", checkedIndicator);

			var checkedState = new VisualState { Name = "Checked" };
			checkedState.Setters.Add(new Setter
			{
				TargetName = "CheckedIndicator",
				Property = VisualElement.OpacityProperty,
				Value = 1d,
			});
			var uncheckedState = new VisualState { Name = "Unchecked" };
			uncheckedState.Setters.Add(new Setter
			{
				TargetName = "CheckedIndicator",
				Property = VisualElement.OpacityProperty,
				Value = 0d,
			});
			var checkedStates = new VisualStateGroup { Name = "CheckedStates" };
			checkedStates.States.Add(checkedState);
			checkedStates.States.Add(uncheckedState);
			VisualStateManager.SetVisualStateGroups(root, new VisualStateGroupList { checkedStates });

			return root;
		}
#endif
	}
}

