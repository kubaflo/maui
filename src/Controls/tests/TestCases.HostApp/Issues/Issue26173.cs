namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26173, "Fancy Sample Code Uses Copyrighted Fonts", PlatformAffected.iOS)]
public class Issue26173 : ContentPage
{
	public Issue26173()
	{
		Title = "New .NET MAUI Project";

		var includeSampleContentCheckBox = new CheckBox
		{
			AutomationId = "IncludeSampleContentCheckBox",
			IsChecked = false
		};
		var checkBoxStateLabel = new Label
		{
			AutomationId = "IncludeSampleContentState",
			Text = "False"
		};
		var completionSequenceLabel = new Label
		{
			AutomationId = "CompletionSequence",
			Text = "-1"
		};
		var generatedFontsHeading = new Label
		{
			AutomationId = "GeneratedFontsHeading",
			FontAttributes = FontAttributes.Bold,
			IsVisible = false,
			Text = "Generated project: Resources/Fonts"
		};
		var fluentFontLabel = new Label
		{
			AutomationId = "FluentFontLabel",
			IsVisible = false,
			Text = "FluentSystemIcons-Regular.ttf"
		};
		var segoeFontLabel = new Label
		{
			AutomationId = "SegoeFontLabel",
			IsVisible = false,
			Text = "SegoeUI-Semibold.ttf"
		};
		var createProjectButton = new Button
		{
			AutomationId = "CreateProjectButton",
			Text = "Create sample project"
		};
		var orientationLabel = new Label
		{
			AutomationId = "OrientationState",
			Text = "Unknown"
		};
		var application = Application.Current;
		var themeLabel = new Label
		{
			AutomationId = "ThemeState",
			Text = application is null ? "Unavailable" : application.RequestedTheme.ToString()
		};

		includeSampleContentCheckBox.CheckedChanged += (_, e) =>
			checkBoxStateLabel.Text = e.Value.ToString();
		createProjectButton.Clicked += (_, _) =>
		{
			if (!includeSampleContentCheckBox.IsChecked)
				return;

			generatedFontsHeading.IsVisible = true;
			fluentFontLabel.IsVisible = true;
			segoeFontLabel.IsVisible = true;
			completionSequenceLabel.Text = "0";
		};
		SizeChanged += (_, _) =>
			orientationLabel.Text = Width <= Height ? "Portrait" : "Landscape";

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 24,
						Text = "Create a new .NET MAUI sample project"
					},
					new Label
					{
						Text = "Choose whether the generated project should include sample content."
					},
					new HorizontalStackLayout
					{
						Spacing = 12,
						Children =
						{
							includeSampleContentCheckBox,
							new Label
							{
								Text = "Include Sample Content",
								VerticalOptions = LayoutOptions.Center
							}
						}
					},
					checkBoxStateLabel,
					createProjectButton,
					generatedFontsHeading,
					fluentFontLabel,
					segoeFontLabel,
					completionSequenceLabel,
					orientationLabel,
					themeLabel
				}
			}
		};
	}
}

