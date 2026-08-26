#if WINDOWS
using WImage = Microsoft.UI.Xaml.Controls.Image;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26094, "Image renders at full window size instead of its intrinsic size on AbsoluteLayout", PlatformAffected.UWP)]
public partial class Issue26094 : ContentPage
{
#if WINDOWS
	readonly HashSet<WImage> _observedNativeImages = [];
	bool _awaitingReplacementImage;
	int _replacementLoadGeneration = -1;
#endif

	public Issue26094()
	{
		InitializeComponent();
	}

	void OnAffectedImageHandlerChanged(object sender, EventArgs e)
	{
#if WINDOWS
		ObserveNativeImage();
#endif
	}

	void OnAffectedImageLoaded(object sender, EventArgs e)
	{
#if WINDOWS
		ObserveNativeImage();
#endif
	}

	void OnSwapClicked(object sender, EventArgs e)
	{
#if WINDOWS
		_replacementLoadGeneration = -1;
		ImageLoadGeneration.Text = _replacementLoadGeneration.ToString();
		_awaitingReplacementImage = true;
#endif
		AffectedImage.Source = "dotnet_bot.png";
		SourceState.Text = "dotnet_bot.png";
	}

#if WINDOWS
	void ObserveNativeImage()
	{
		if (AffectedImage.Handler?.PlatformView is not WImage nativeImage ||
			!_observedNativeImages.Add(nativeImage))
		{
			return;
		}

		nativeImage.ImageOpened += (_, _) =>
		{
			if (!_awaitingReplacementImage)
			{
				return;
			}

			_awaitingReplacementImage = false;
			_replacementLoadGeneration++;
			ImageLoadGeneration.Text = _replacementLoadGeneration.ToString();
		};
	}
#endif
}
