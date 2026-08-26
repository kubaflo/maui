namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35402, "Bundled MauiFont is registered twice at launch on iOS 26", PlatformAffected.iOS)]
public class Issue35402 : ContentPage
{
	public Issue35402()
	{
		var nativeFontStatusLabel = new Label
		{
			AutomationId = "Issue35402NativeFontStatus",
			HorizontalTextAlignment = TextAlignment.Center,
			Text = "Pending",
		};

		var bundledFontLabel = new Label
		{
			AutomationId = "Issue35402BundledFont",
			FontFamily = "OpenSansRegular",
			FontSize = 42,
			HorizontalTextAlignment = TextAlignment.Center,
			Text = "OpenSans bundled font",
		};

#if IOS
		void PublishNativeFont()
		{
			if (bundledFontLabel.Handler?.PlatformView is UIKit.UILabel nativeLabel &&
				nativeLabel.Font is not null)
			{
				nativeFontStatusLabel.Text = nativeLabel.Font.Name;
			}
		}

		bundledFontLabel.HandlerChanged += (_, _) => PublishNativeFont();
		bundledFontLabel.Loaded += (_, _) => PublishNativeFont();
#endif

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				bundledFontLabel,
				nativeFontStatusLabel,
			},
		};
	}
}

