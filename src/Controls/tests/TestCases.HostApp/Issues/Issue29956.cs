namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29956, "ImageButton border is clipped when AspectFill is selected", PlatformAffected.Android)]
public class Issue29956 : ContentPage
{
	public Issue29956()
	{
		var captureTokenLabel = new Label
		{
			AutomationId = "CaptureToken",
			Text = "-1",
			FontSize = 1,
			Opacity = 0.01
		};
		var transitionCountLabel = new Label
		{
			AutomationId = "TransitionCount",
			Text = "0",
			FontSize = 1,
			Opacity = 0.01
		};
		var aspectStateLabel = new Label
		{
			AutomationId = "AspectState",
			Text = "Current aspect: AspectFit",
			HorizontalOptions = LayoutOptions.Center
		};
		var resultLabel = new Label
		{
			AutomationId = "ResultStatus",
			Text = "Not recorded",
			FontAttributes = FontAttributes.Bold,
			HorizontalOptions = LayoutOptions.Center
		};
		var affectedImageButton = new ImageButton
		{
			AutomationId = "AffectedImageButton",
			Source = "dotnet_bot.png",
			Aspect = Aspect.AspectFit,
			BorderColor = Colors.Red,
			BorderWidth = 8,
			HeightRequest = 180,
			WidthRequest = 260,
			HorizontalOptions = LayoutOptions.Center
		};

		void MarkInitialRenderReady()
		{
			if (!affectedImageButton.IsLoading)
				captureTokenLabel.Text = "0";
		}

		affectedImageButton.Loaded += (_, _) => MarkInitialRenderReady();
		affectedImageButton.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(ImageButton.IsLoading))
				MarkInitialRenderReady();
		};

		var aspectFillRadioButton = new RadioButton
		{
			AutomationId = "AspectFillRadioButton",
			Content = "AspectFill",
			GroupName = "Aspect"
		};
		aspectFillRadioButton.CheckedChanged += (_, e) =>
		{
			if (!e.Value)
				return;

			affectedImageButton.Aspect = Aspect.AspectFill;
			transitionCountLabel.Text = "1";
			aspectStateLabel.Text = "Current aspect: AspectFill";
			Dispatcher.Dispatch(() => captureTokenLabel.Text = "1");
		};

		var recordButton = new Button
		{
			AutomationId = "RecordButton",
			Text = "Record missing side borders",
			HorizontalOptions = LayoutOptions.Center
		};
		recordButton.Clicked += (_, _) =>
		{
			if (affectedImageButton.Aspect == Aspect.AspectFill &&
				transitionCountLabel.Text == "1" &&
				captureTokenLabel.Text == "1")
			{
				resultLabel.Text = "Recorded";
			}
		};

		var headingLabel = new Label
		{
			Text = "ImageButton border with file image",
			FontAttributes = FontAttributes.Bold,
			FontSize = 20
		};
		var instructionLabel = new Label
		{
			Text = "Select AspectFill and inspect all four red border edges."
		};
		var aspectOptions = new HorizontalStackLayout
		{
			Spacing = 24,
			HorizontalOptions = LayoutOptions.Center,
			Children =
			{
				new RadioButton
				{
					Content = "AspectFit",
					GroupName = "Aspect",
					IsChecked = true
				},
				aspectFillRadioButton
			}
		};
		var resultGrid = new Grid
		{
			Children =
			{
				resultLabel,
				captureTokenLabel,
				transitionCountLabel
			}
		};

		Grid.SetRow(instructionLabel, 1);
		Grid.SetRow(affectedImageButton, 2);
		Grid.SetRow(aspectStateLabel, 3);
		Grid.SetRow(aspectOptions, 4);
		Grid.SetRow(recordButton, 5);
		Grid.SetRow(resultGrid, 6);

		Content = new Grid
		{
			Padding = 24,
			RowSpacing = 16,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(180),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				headingLabel,
				instructionLabel,
				affectedImageButton,
				aspectStateLabel,
				aspectOptions,
				recordButton,
				resultGrid
			}
		};
	}
}

