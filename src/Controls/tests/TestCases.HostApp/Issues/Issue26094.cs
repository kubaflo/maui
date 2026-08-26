#if WINDOWS
using AbsoluteLayoutFlags = Microsoft.Maui.Layouts.AbsoluteLayoutFlags;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26094, "Image renders at full window size instead of actual image size on AbsoluteLayout", PlatformAffected.UWP)]
public class Issue26094 : ContentPage
{
	public Issue26094()
	{
		var affectedImage = new Image
		{
			AutomationId = "Issue26094AffectedImage",
			Source = "shopping_cart.png",
			Aspect = Aspect.AspectFill,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};

		AbsoluteLayout.SetLayoutBounds(affectedImage, new Rect(0, 0, 1, 1));
		AbsoluteLayout.SetLayoutFlags(affectedImage, AbsoluteLayoutFlags.All);

		var affectedLayout = new AbsoluteLayout
		{
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill,
			Children = { affectedImage }
		};

		var calibrationImage = new Image
		{
			AutomationId = "Issue26094CalibrationImage",
			Source = "shopping_cart.png",
			WidthRequest = 44,
			HeightRequest = 44
		};

		Content = new StackLayout
		{
			Children =
			{
				affectedLayout,
				calibrationImage
			}
		};
	}
}
#endif

