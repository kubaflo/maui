namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35410, "Text is obscured by the notch after counter-clockwise rotation",
	PlatformAffected.iOS)]
public class Issue35410 : NavigationPage
{
	public Issue35410() : base(new Issue35410ContentPage())
	{
	}
}

public partial class Issue35410ContentPage : ContentPage
{
	public Issue35410ContentPage()
	{
		InitializeComponent();

		SentenceEditor.Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeSentence);
		WordEditor.Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeWord);
		CharacterEditor.Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeCharacter);
		NoneEditor.Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeNone);
		SpellcheckEditor.Keyboard = Keyboard.Create(KeyboardFlags.Spellcheck);

		SizeChanged += OnLayoutSizeChanged;
		FirstInstruction.SizeChanged += OnLayoutSizeChanged;
		PropertyChanged += OnPagePropertyChanged;
	}

	void OnPagePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
#if IOS
		if (e.PropertyName == Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SafeAreaInsetsProperty.PropertyName)
			OnLayoutSizeChanged(sender, EventArgs.Empty);
#endif
	}

	void OnLayoutSizeChanged(object sender, EventArgs e)
	{
#if IOS
		if (FirstInstruction.Handler?.PlatformView is not UIKit.UILabel nativeLabel ||
			nativeLabel.Window is not UIKit.UIWindow nativeWindow)
		{
			return;
		}

		var windowBounds = nativeWindow.Bounds;
		if (windowBounds.Width <= windowBounds.Height)
		{
			Title = "PORTRAIT_LAYOUT_COMPLETE";
			return;
		}

		var safeAreaInsets = nativeWindow.SafeAreaInsets;
		if (safeAreaInsets.Left <= 0)
			return;

		var instructionFrame = nativeLabel.ConvertRectToView(nativeLabel.Bounds, nativeWindow);
		Title = FormattableString.Invariant(
			$"LANDSCAPE_LAYOUT_COMPLETE|{windowBounds.Width:0.###}|{windowBounds.Height:0.###}|{safeAreaInsets.Left:0.###}|{instructionFrame.X:0.###}|{instructionFrame.Y:0.###}|{instructionFrame.Width:0.###}|{instructionFrame.Height:0.###}");
#endif
	}
}
