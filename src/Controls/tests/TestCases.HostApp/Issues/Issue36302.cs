#if IOS
using System.Globalization;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36302, "Image and ImageButton BackgroundColor does not reset when set to null", PlatformAffected.iOS)]
public class Issue36302 : ContentPage
{
	readonly Image affectedImage;
	readonly ImageButton affectedImageButton;
	readonly Image referenceImage;
	readonly ImageButton referenceImageButton;
	readonly Label imageState;
	readonly Label imageButtonState;
	readonly Label referenceState;

	public Issue36302()
	{
		affectedImage = new Image
		{
			AutomationId = "AffectedImage",
			Source = "dotnet_bot.png",
			BackgroundColor = Colors.Blue,
			HeightRequest = 90,
			WidthRequest = 150,
			HorizontalOptions = LayoutOptions.Center
		};

		affectedImageButton = new ImageButton
		{
			AutomationId = "AffectedImageButton",
			Source = "dotnet_bot.png",
			BackgroundColor = Colors.Blue,
			HeightRequest = 90,
			WidthRequest = 150,
			HorizontalOptions = LayoutOptions.Center
		};

		referenceImage = new Image
		{
			AutomationId = "ReferenceImage",
			HeightRequest = 1,
			WidthRequest = 1
		};

		referenceImageButton = new ImageButton
		{
			AutomationId = "ReferenceImageButton",
			HeightRequest = 1,
			WidthRequest = 1
		};

		var applyRedButton = new Button
		{
			AutomationId = "ApplyRedButton",
			Text = "Apply red backgrounds"
		};

		var clearBackgroundButton = new Button
		{
			AutomationId = "ClearBackgroundButton",
			Text = "Set backgrounds to null"
		};

		imageState = CreateStateLabel("ImageState");
		imageButtonState = CreateStateLabel("ImageButtonState");
		referenceState = CreateStateLabel("ReferenceState");

#if IOS
		applyRedButton.Clicked += (_, _) =>
		{
			affectedImage.BackgroundColor = Colors.Red;
			affectedImageButton.BackgroundColor = Colors.Red;
			Dispatcher.Dispatch(() => RecordState(1));
		};

		clearBackgroundButton.Clicked += (_, _) =>
		{
			affectedImage.BackgroundColor = null;
			affectedImageButton.BackgroundColor = null;
			Dispatcher.Dispatch(() => RecordState(2));
		};

		Loaded += (_, _) => Dispatcher.Dispatch(() => RecordState(-1));
#endif

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 12,
			Children =
			{
				affectedImage,
				affectedImageButton,
				applyRedButton,
				clearBackgroundButton,
				referenceImage,
				referenceImageButton,
				imageState,
				imageButtonState,
				referenceState
			}
		};
	}

	static Label CreateStateLabel(string automationId) => new()
	{
		AutomationId = automationId,
		FontSize = 1,
		LineHeight = 1,
		Text = "generation=unobserved"
	};

#if IOS
	void RecordState(int generation)
	{
		imageState.Text = Describe(affectedImage, generation);
		imageButtonState.Text = Describe(affectedImageButton, generation);
		referenceState.Text = $"generation={generation};image={GetNativeState(referenceImage).Rgba};imageButton={GetNativeState(referenceImageButton).Rgba}";
	}

	static string Describe(Image image, int generation)
	{
		var backgroundColor = image.BackgroundColor;
		var managedColor = backgroundColor is null ? "null" : DescribeManagedColor(backgroundColor);
		var nativeState = GetNativeState(image);
		return $"generation={generation};handler={image.Handler?.GetType().Name};attached={image.Handler is Microsoft.Maui.Handlers.ImageHandler};window={nativeState.HasWindow};sourceConfigured={image.Source is not null};bounds={Format(image.Width)}x{Format(image.Height)};managed={managedColor};rgba={nativeState.Rgba}";
	}

	static string Describe(ImageButton imageButton, int generation)
	{
		var backgroundColor = imageButton.BackgroundColor;
		var managedColor = backgroundColor is null ? "null" : DescribeManagedColor(backgroundColor);
		var nativeState = GetNativeState(imageButton);
		return $"generation={generation};handler={imageButton.Handler?.GetType().Name};attached={imageButton.Handler is Microsoft.Maui.Handlers.ImageButtonHandler};window={nativeState.HasWindow};sourceConfigured={imageButton.Source is not null};bounds={Format(imageButton.Width)}x{Format(imageButton.Height)};managed={managedColor};rgba={nativeState.Rgba}";
	}

	static (bool HasWindow, string Rgba) GetNativeState(Image image)
	{
		if (image.Handler is not Microsoft.Maui.Handlers.ImageHandler handler)
			return (false, GetRgba(UIKit.UIColor.Clear));

		var platformView = handler.PlatformView;
		if (platformView is not null)
			return (platformView.Window is not null, GetRgba(platformView.BackgroundColor ?? UIKit.UIColor.Clear));

		return (false, GetRgba(UIKit.UIColor.Clear));
	}

	static (bool HasWindow, string Rgba) GetNativeState(ImageButton imageButton)
	{
		if (imageButton.Handler is not Microsoft.Maui.Handlers.ImageButtonHandler handler)
			return (false, GetRgba(UIKit.UIColor.Clear));

		var platformView = handler.PlatformView;
		if (platformView is not null)
			return (platformView.Window is not null, GetRgba(platformView.BackgroundColor ?? UIKit.UIColor.Clear));

		return (false, GetRgba(UIKit.UIColor.Clear));
	}

	static string DescribeManagedColor(Color color)
	{
		if (color.Equals(Colors.Blue))
			return "Blue";
		if (color.Equals(Colors.Red))
			return "Red";
		return color.ToString();
	}

	static string GetRgba(UIKit.UIColor color)
	{
		color.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return $"{Format(red)},{Format(green)},{Format(blue)},{Format(alpha)}";
	}

	static string Format(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);
#endif
}

