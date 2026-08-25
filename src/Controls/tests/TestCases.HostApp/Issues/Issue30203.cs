namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30203, "[Windows] Navigation briefly exposes the window background", PlatformAffected.UWP)]
public class Issue30203 : NavigationPage
{
	static readonly Color PageColor = Color.FromArgb("#F2E85C");

	readonly Label _firstPageStatusLabel;
	int _cycleNumber;

	public Issue30203() : this(new ContentPage())
	{
	}

	Issue30203(ContentPage firstPage) : base(firstPage)
	{
		_firstPageStatusLabel = CreateStatusLabel("FirstTransitionResult", "PASS:");
		var navigateButton = new Button
		{
			AutomationId = "NavigateButton",
			Text = "Navigate to second page"
		};
		navigateButton.Clicked += OnNavigateClicked;

		firstPage.AutomationId = "FirstPage";
		firstPage.Title = "First page";
		firstPage.BackgroundColor = PageColor;
		firstPage.Content = CreateStack(
			CreateLabel("FirstHeading", "First page - yellow application background", 28),
			CreateLabel("FirstDescription", "Both pages use the same background. Watch the full window during the animated transition.", 18),
			_firstPageStatusLabel,
			navigateButton);
		firstPage.Loaded += (sender, args) =>
			_firstPageStatusLabel.Text = $"PASS: Ready for cycle {_cycleNumber + 1}";
	}

	static Label CreateLabel(string automationId, string text, double fontSize) => new()
	{
		AutomationId = automationId,
		Text = text,
		FontSize = fontSize,
		HorizontalTextAlignment = TextAlignment.Center
	};

	static Label CreateStatusLabel(string automationId, string text) => new()
	{
		AutomationId = automationId,
		Text = text,
		FontSize = 20,
		FontAttributes = FontAttributes.Bold,
		HorizontalTextAlignment = TextAlignment.Center
	};

	static VerticalStackLayout CreateStack(params View[] children)
	{
		var stack = new VerticalStackLayout
		{
			Padding = 32,
			Spacing = 24,
			VerticalOptions = LayoutOptions.Center
		};

		foreach (var child in children)
			stack.Children.Add(child);

		return stack;
	}

	async void OnNavigateClicked(object sender, EventArgs e)
	{
		var destination = CreateSecondPage();
		await Navigation.PushAsync(destination.Page, true);
		_cycleNumber++;
		destination.Status.Text =
			$"Cycle {_cycleNumber} complete; navigation surface background={GetNavigationSurfaceBackground()}";
	}

	string GetNavigationSurfaceBackground()
	{
#if WINDOWS
		var nativeElement = Handler?.PlatformView as Microsoft.UI.Xaml.DependencyObject;
		while (nativeElement is not null)
		{
			Microsoft.UI.Xaml.Media.Brush background = nativeElement switch
			{
				Microsoft.UI.Xaml.Controls.Border border => border.Background,
				Microsoft.UI.Xaml.Controls.Panel panel => panel.Background,
				Microsoft.UI.Xaml.Controls.Control control => control.Background,
				_ => null
			};

			if (background is Microsoft.UI.Xaml.Media.SolidColorBrush brush)
			{
				if (brush.Color.A > 0)
				{
					var color = brush.Color;
					return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
				}
			}
			else if (background is not null && background.Opacity > 0)
			{
				return background.GetType().Name;
			}

			nativeElement = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(nativeElement);
		}

		return "transparent";
#else
		return "unsupported";
#endif
	}

	(ContentPage Page, Label Status) CreateSecondPage()
	{
		var status = CreateStatusLabel("SecondTransitionResult", "PASS:");
		var returnButton = new Button
		{
			AutomationId = "ReturnButton",
			Text = "Return to first page"
		};
		returnButton.Clicked += async (sender, args) =>
		{
			await Navigation.PopAsync(true);
			_firstPageStatusLabel.Text = $"PASS: Ready for cycle {_cycleNumber + 1}";
		};

		var page = new ContentPage
		{
			AutomationId = "SecondPage",
			Title = "Second page",
			BackgroundColor = PageColor,
			Content = CreateStack(
				CreateLabel("SecondHeading", "Second page - yellow application background", 28),
				CreateLabel("SecondDescription", "The dark purple frame shown during navigation is outside both page backgrounds.", 18),
				status,
				returnButton)
		};

		return (page, status);
	}
}

