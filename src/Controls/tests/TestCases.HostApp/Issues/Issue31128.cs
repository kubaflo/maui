#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31128, "IndicatorTemplate does not update when set dynamically without an initial value", PlatformAffected.Android)]
public class Issue31128 : ContentPage
{
	const string CustomIndicatorId = "Issue31128CustomIndicator";

	public Issue31128()
	{
		var indicatorView = new IndicatorView
		{
			AutomationId = "Issue31128IndicatorView",
			Count = 4,
			Position = 1,
			HorizontalOptions = LayoutOptions.Center
		};

		var applyTemplateButton = new Button
		{
			AutomationId = "Issue31128ApplyTemplateButton",
			Text = "Apply dynamic template"
		};

		var layout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 20,
			Children =
			{
				new Label
				{
					FontSize = 22,
					Text = "Dynamic IndicatorTemplate reproduction"
				},
				new Label
				{
					Text = "Before applying the template, four platform-default dots are shown. After applying it, four wide CUSTOM indicators should replace them."
				},
				indicatorView,
				applyTemplateButton
			}
		};

		applyTemplateButton.Clicked += (sender, e) =>
		{
			indicatorView.IndicatorTemplate = new DataTemplate(() =>
			{
				var templateRoot = new Grid
				{
					WidthRequest = 52,
					HeightRequest = 28
				};

				templateRoot.Children.Add(new Label
				{
					AutomationId = CustomIndicatorId,
					Text = "CUSTOM",
					TextColor = Colors.White,
					FontSize = 10,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				});

				return templateRoot;
			});
			applyTemplateButton.Text = "Template applied";
		};

		Content = layout;
	}
}
#endif

