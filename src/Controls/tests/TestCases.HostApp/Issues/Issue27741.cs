namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27741, "Screen not using the full width by default when on locked device orientation", PlatformAffected.iOS)]
public class Issue27741 : ContentPage
{
#if IOS
	int _layoutGeneration = -1;
	bool _isObservingLayout;
	readonly Grid _issueRootLayout;
	readonly Label _layoutGenerationLabel;

	public Issue27741()
	{
		Title = "Landscape width";

		_layoutGenerationLabel = new Label
		{
			AutomationId = "LayoutGeneration",
			Text = _layoutGeneration.ToString(),
			TextColor = Colors.Black
		};

		var header = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(260),
				new ColumnDefinition(GridLength.Star)
			},
			HeightRequest = 72
		};
		header.Add(new Label
		{
			Text = "MAUI landscape",
			BackgroundColor = Color.FromArgb("#081A3A"),
			TextColor = Colors.White,
			FontSize = 24,
			VerticalTextAlignment = TextAlignment.Center,
			Padding = new Thickness(24, 0)
		});
		header.Add(new Label
		{
			Text = "Full-width reference",
			BackgroundColor = Colors.White,
			TextColor = Color.FromArgb("#111111"),
			FontSize = 22,
			VerticalTextAlignment = TextAlignment.Center,
			Padding = new Thickness(24, 0)
		}, 1);

		var affectedContent = new VerticalStackLayout
		{
			Padding = 36,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Landscape content",
					FontSize = 40,
					FontAttributes = FontAttributes.Bold,
					TextColor = Color.FromArgb("#111111")
				},
				new Label
				{
					Text = "This turquoise surface must reach the right edge.",
					FontSize = 22,
					TextColor = Color.FromArgb("#111111")
				},
				new BoxView
				{
					HeightRequest = 32,
					HorizontalOptions = LayoutOptions.Fill,
					Color = Color.FromArgb("#94008C")
				}
			}
		};

		var affectedSurface = new Grid
		{
			AutomationId = "AffectedSurface",
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill,
			ColumnDefinitions =
			{
				new ColumnDefinition(260),
				new ColumnDefinition(GridLength.Star)
			}
		};
		affectedSurface.Add(new BoxView { Color = Color.FromArgb("#22104D") });
		affectedSurface.Add(affectedContent, 1);

		var footer = new Grid
		{
			BackgroundColor = Colors.White,
			Padding = new Thickness(12, 8),
			ColumnSpacing = 16,
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Star)
			}
		};
		footer.Add(new Label
		{
			Text = "Layout generation:",
			FontAttributes = FontAttributes.Bold,
			TextColor = Color.FromArgb("#111111")
		});
		footer.Add(_layoutGenerationLabel, 1);

		_issueRootLayout = new Grid
		{
			AutomationId = "RootLayout",
			BackgroundColor = Color.FromArgb("#72EBCB"),
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		_issueRootLayout.Add(header);
		_issueRootLayout.Add(affectedSurface, 0, 1);
		_issueRootLayout.Add(footer, 0, 2);
		Content = _issueRootLayout;
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();

		if (Handler is not null && !_isObservingLayout)
		{
			_isObservingLayout = true;
			_issueRootLayout.SizeChanged += OnRootLayoutSizeChanged;
		}
	}

	void OnRootLayoutSizeChanged(object sender, EventArgs e)
	{
		if (_issueRootLayout.Width <= 0 || _issueRootLayout.Height <= 0)
			return;

		_layoutGeneration = _layoutGeneration < 0 ? 1 : _layoutGeneration + 1;
		_layoutGenerationLabel.Text = $"Complete:{_layoutGeneration}";
	}
#else
	public Issue27741()
	{
		Content = new Label { Text = "This test is only applicable to iOS." };
	}
#endif
}

