using Microsoft.Maui.Handlers;

namespace Maui.Controls.Sample.Issues;

public partial class Issue30203
{
	string GetFrameBackground()
	{
		if (Handler is not NavigationViewHandler navigationHandler)
			return "NoNavigationHandler";

		if (navigationHandler.PlatformView.Background is not Microsoft.UI.Xaml.Media.SolidColorBrush brush)
			return "Transparent";

		var color = brush.Color;
		return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}
}
