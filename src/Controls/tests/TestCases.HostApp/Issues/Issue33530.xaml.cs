namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33530, "[Android] Initially rotated Border with Start alignment is positioned incorrectly", PlatformAffected.Android)]
public partial class Issue33530 : ContentPage
{
	public Issue33530()
	{
		InitializeComponent();
	}

	async void OnOpenCleanClicked(object sender, EventArgs e)
	{
		await Navigation.PushModalAsync(CreateModalPage("CleanPageTemplate"), false);
	}

	async void OnOpenAffectedClicked(object sender, EventArgs e)
	{
		await Navigation.PushModalAsync(CreateModalPage("AffectedPageTemplate"), false);
	}

	async void OnCloseCleanClicked(object sender, EventArgs e)
	{
		await Navigation.PopModalAsync(false);
	}

	void OnCleanPageLoaded(object sender, EventArgs e)
	{
		var modalPage = GetModalPage(sender);
		if (modalPage.Content is not Border border)
			throw new InvalidOperationException("The clean modal must contain a Border.");

		if (border.Width <= 0 || border.Height <= 0)
		{
			border.SizeChanged += OnCleanBorderSizeChanged;
			return;
		}

		ConfigureCleanBorder(modalPage, border);
	}

	void OnCleanBorderSizeChanged(object sender, EventArgs e)
	{
		if (sender is not Border border)
			throw new InvalidOperationException("The SizeChanged sender must be a Border.");

		if (border.Width <= 0 || border.Height <= 0)
			return;

		if (border.Parent is not ContentPage modalPage)
			throw new InvalidOperationException("The clean Border must be the modal page's direct child.");

		border.SizeChanged -= OnCleanBorderSizeChanged;
		ConfigureCleanBorder(modalPage, border);
	}

	static void ConfigureCleanBorder(ContentPage modalPage, Border border)
	{
		var status = modalPage.FindByName<Label>("CleanLoadedStatus");
		if (status is null)
			throw new InvalidOperationException("The clean modal status label was not created.");

		border.Rotation = -90;
		border.HorizontalOptions = LayoutOptions.Start;
		status.Text = "LOADED Rotation=-90 HorizontalOptions=Start";
	}

	void OnAffectedPageLoaded(object sender, EventArgs e)
	{
		var modalPage = GetModalPage(sender);
		var status = modalPage.FindByName<Label>("AffectedLoadedStatus");
		if (status is null)
			throw new InvalidOperationException("The affected modal status label was not created.");

		status.Text = "LOADED Rotation=-90 HorizontalOptions=Start";
	}

	void OnAffectedLayoutCheckClicked(object sender, EventArgs e)
	{
		if (sender is not Button button ||
			button.Parent is not VerticalStackLayout content ||
			content.Parent is not Border border ||
			border.Parent is not ContentPage modalPage)
		{
			throw new InvalidOperationException("The layout check must remain inside the modal Border.");
		}

#if ANDROID
		if (border.Handler is not Microsoft.Maui.IViewHandler borderHandler ||
			modalPage.Handler?.PlatformView is not Android.Views.View pageView)
		{
			throw new InvalidOperationException("The Android views required for transformed-bounds measurement were not created.");
		}

		var borderView = borderHandler.ContainerView as Android.Views.View ??
			borderHandler.PlatformView as Android.Views.View;
		if (borderView is null)
			throw new InvalidOperationException("The Border platform view was not created.");

		var borderBounds = GetTransformedBounds(borderView);
		var pageBounds = GetTransformedBounds(pageView);
		var offscreenPixels = pageBounds.Left - borderBounds.Left;
		var result = Math.Abs(offscreenPixels) <= 4 ? "ALIGNED" : "MISALIGNED";
		var layoutResult = modalPage.FindByName<Label>("AffectedLayoutResult");
		if (layoutResult is null)
			throw new InvalidOperationException("The affected modal layout result label was not created.");

		layoutResult.Text = FormattableString.Invariant(
			$"{result}: borderLeft={borderBounds.Left:0.###}, pageLeft={pageBounds.Left:0.###}, offscreen={offscreenPixels:0.###}");
#endif
	}

#if ANDROID
	static Android.Graphics.RectF GetTransformedBounds(Android.Views.View view)
	{
		var bounds = new Android.Graphics.RectF(0, 0, view.Width, view.Height);
		Android.Views.View current = view;

		while (current is not null)
		{
			current.Matrix?.MapRect(bounds);

			if (current.Parent is Android.Views.View parent)
			{
				bounds.Offset(current.Left - parent.ScrollX, current.Top - parent.ScrollY);
				current = parent;
			}
			else
			{
				bounds.Offset(current.Left, current.Top);
				current = null;
			}
		}

		return bounds;
	}
#endif

	ContentPage CreateModalPage(string resourceKey)
	{
		if (Resources[resourceKey] is not DataTemplate template)
			throw new InvalidOperationException($"The '{resourceKey}' template was not found.");

		if (template.CreateContent() is not ContentPage modalPage)
			throw new InvalidOperationException($"The '{resourceKey}' template did not create a ContentPage.");

		return modalPage;
	}

	static ContentPage GetModalPage(object sender)
	{
		if (sender is not ContentPage modalPage)
			throw new InvalidOperationException("The Loaded sender must be a ContentPage.");

		return modalPage;
	}
}
