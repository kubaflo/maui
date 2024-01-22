namespace Microsoft.Maui;

/// <summary>
/// Provides functionality for requesting hidding home indicator on the device screen.
/// </summary>
/// <remarks>
/// This interface may be applied to IContentView.
/// This interface is only recognized on the iOS/Mac Catalyst platforms; other platforms will ignore it.
/// </remarks>
public interface IHomeIndicatorAutoHiddenView
{
	/// <summary>
	/// Sets a Boolean value that, if true hides HomeIndicator
	/// </summary>
	bool IsHomeIndicatorAutoHidden { get; }
}

