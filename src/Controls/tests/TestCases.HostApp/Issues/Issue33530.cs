namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33530, "[Android] Initially rotated Border with Start alignment is positioned incorrectly", PlatformAffected.Android)]
public class Issue33530 : ContentPage
{
	const string ContentText = "Rotated Border Content";

	public Issue33530()
	{
		var initiallyRotatedButton = new Button
		{
			Text = "Open initially rotated border",
			AutomationId = "Issue33530InitiallyRotatedButton"
		};
		initiallyRotatedButton.Clicked += OnOpenInitiallyRotatedClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "Android rotated Border initial layout",
					AutomationId = "Issue33530PageReady",
					FontSize = 20
				},
				initiallyRotatedButton
			}
		};
	}

	async void OnOpenInitiallyRotatedClicked(object sender, EventArgs e)
	{
		var statusLabel = CreateStatusLabel("Issue33530TargetStatus");
		var border = CreateBorder(
			"Issue33530TargetBorder",
			"Issue33530TargetContent",
			statusLabel,
			LayoutOptions.Start,
			-90);

		var isLoaded = false;
		border.Loaded += OnLoaded;
		border.SizeChanged += OnSizeChanged;
		await Navigation.PushModalAsync(CreateModal(border), false);

		void OnLoaded(object loadedSender, EventArgs args)
		{
			isLoaded = true;
			TryRecordVisualLeft();
		}

		void OnSizeChanged(object sizeChangedSender, EventArgs args)
		{
			if (isLoaded)
				TryRecordVisualLeft();
		}

		void TryRecordVisualLeft()
		{
			if (border.Width <= 0 || border.Height <= 0)
				return;

#if ANDROID
			if (border.Handler?.PlatformView is not Android.Views.View platformView)
				return;

			if (!OperatingSystem.IsAndroidVersionAtLeast(29))
				throw new PlatformNotSupportedException("Issue33530 requires Android API 29 or later to inspect the rendered transform.");

			if (platformView.Width <= 0 || platformView.Height <= 0)
				return;

			using var globalTransform = new Android.Graphics.Matrix();
			platformView.TransformMatrixToGlobal(globalTransform);
			using var transformedBounds = new Android.Graphics.RectF(0, 0, platformView.Width, platformView.Height);
			globalTransform.MapRect(transformedBounds);

			border.Loaded -= OnLoaded;
			border.SizeChanged -= OnSizeChanged;
			statusLabel.Text = $"READY:{transformedBounds.Left.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}";
#endif
		}
	}

	static ContentPage CreateModal(Border border)
	{
		return new ContentPage
		{
			BackgroundColor = Colors.Transparent,
			Content = border
		};
	}

	static Label CreateStatusLabel(string automationId)
	{
		return new Label
		{
			Text = "WAITING",
			AutomationId = automationId,
			WidthRequest = 260
		};
	}

	static Border CreateBorder(
		string borderAutomationId,
		string contentAutomationId,
		Label statusLabel,
		LayoutOptions horizontalOptions,
		double rotation)
	{
		return new Border
		{
			AutomationId = borderAutomationId,
			BackgroundColor = Colors.White,
			HorizontalOptions = horizontalOptions,
			VerticalOptions = LayoutOptions.Center,
			Rotation = rotation,
			Padding = 20,
			Stroke = Colors.DarkBlue,
			StrokeThickness = 3,
			Shadow = new Shadow
			{
				Brush = Colors.Red,
				Offset = new Point(12, 12),
				Opacity = 0.9f,
				Radius = 8
			},
			Content = new VerticalStackLayout
			{
				Spacing = 8,
				Children =
				{
					new BoxView
					{
						Color = Colors.LightBlue,
						HeightRequest = 80,
						WidthRequest = 260
					},
					new Label
					{
						Text = ContentText,
						AutomationId = contentAutomationId,
						FontAttributes = FontAttributes.Bold,
						FontSize = 18
					},
					statusLabel
				}
			}
		};
	}
}

