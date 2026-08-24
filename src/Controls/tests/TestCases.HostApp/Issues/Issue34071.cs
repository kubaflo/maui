#if WINDOWS
using WAppBarButton = Microsoft.UI.Xaml.Controls.AppBarButton;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;
using WWindow = Microsoft.UI.Xaml.Window;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34071, "Shell foreground color is not applied to toolbar items", PlatformAffected.UWP)]
public class Issue34071 : ContentPage
{
	public Issue34071()
	{
		var launchButton = new Button
		{
			AutomationId = "Issue34071LaunchShellButton",
			Text = "Launch Shell"
		};

		launchButton.Clicked += (_, _) => LaunchShell();

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "Windows Shell toolbar foreground color",
					FontSize = 24,
					HorizontalTextAlignment = TextAlignment.Center
				},
				launchButton
			}
		};
	}

	void LaunchShell()
	{
		var referenceLabel = new Label
		{
			AutomationId = "Issue34071ReferenceLabel",
			Text = "Expected toolbar icon color: PURPLE",
			TextColor = Colors.Purple,
			FontSize = 22
		};

		var measurementLabel = new Label
		{
			AutomationId = "Issue34071MeasurementLabel",
			Text = "PENDING"
		};

		var checkButton = new Button
		{
			AutomationId = "Issue34071CheckToolbarButton",
			Text = "Check toolbar item color"
		};

		var page = new ContentPage
		{
			Title = "Toolbar Color",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					referenceLabel,
					new Label
					{
						Text = "Observe the shopping-cart icon in the top-right toolbar."
					},
					measurementLabel,
					checkButton
				}
			}
		};

		var toolbarItem = new ToolbarItem
		{
			AutomationId = "Issue34071AffectedToolbarItem",
			IconImageSource = "shopping_cart.png",
			Order = ToolbarItemOrder.Primary
		};
		page.ToolbarItems.Add(toolbarItem);

		var shell = new Shell();
		Shell.SetForegroundColor(shell, Colors.Purple);
		shell.Items.Add(new ShellContent
		{
			Title = "Toolbar Color",
			Content = page
		});

		checkButton.Clicked += (_, _) => shell.Dispatcher.Dispatch(() =>
			PublishNativeColorMeasurement(shell, toolbarItem, referenceLabel, measurementLabel));

		GetParentWindow().Page = shell;
	}

	static void PublishNativeColorMeasurement(
		Shell shell,
		ToolbarItem toolbarItem,
		Label referenceLabel,
		Label measurementLabel)
	{
		if (TryMeasureNativeColors(shell, toolbarItem, referenceLabel, out var observed, out var expected))
		{
			measurementLabel.Text = $"observed={FormatColor(observed)};expected={FormatColor(expected)}";
		}
		else
		{
			measurementLabel.Text = "MEASUREMENT FAILED";
		}
	}

	static bool TryMeasureNativeColors(
		Shell shell,
		ToolbarItem toolbarItem,
		Label referenceLabel,
		out Windows.UI.Color observed,
		out Windows.UI.Color expected)
	{
		observed = default;
		expected = default;

		if (shell.Window?.Handler?.PlatformView is not WWindow platformWindow ||
			platformWindow.Content is not WFrameworkElement windowContent ||
			!windowContent.IsLoaded ||
			windowContent.XamlRoot is not { } xamlRoot ||
			xamlRoot.Size.Width <= 0 ||
			xamlRoot.Size.Height <= 0)
		{
			return false;
		}

		var appBarButton = FindDescendant<WAppBarButton>(
			windowContent,
			button => ReferenceEquals(button.DataContext, toolbarItem));
		if (appBarButton is null ||
			!appBarButton.IsLoaded ||
			appBarButton.ActualWidth <= 0 ||
			appBarButton.ActualHeight <= 0 ||
			appBarButton.Foreground is not WSolidColorBrush toolbarBrush)
		{
			return false;
		}

		var toolbarImage = FindDescendant<WImage>(
			appBarButton,
			image => image.Source is not null && image.IsLoaded);
		if (toolbarImage is null || toolbarImage.ActualWidth <= 0 || toolbarImage.ActualHeight <= 0)
		{
			return false;
		}

		if (referenceLabel.Handler?.PlatformView is not WTextBlock referenceTextBlock ||
			!referenceTextBlock.IsLoaded ||
			referenceTextBlock.Foreground is not WSolidColorBrush referenceBrush)
		{
			return false;
		}

		observed = toolbarBrush.Color;
		expected = referenceBrush.Color;
		return true;
	}

	static T FindDescendant<T>(WDependencyObject root, Predicate<T> predicate)
		where T : WDependencyObject
	{
		var childCount = WVisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < childCount; i++)
		{
			var child = WVisualTreeHelper.GetChild(root, i);
			if (child is T match && predicate(match))
			{
				return match;
			}

			var descendant = FindDescendant(child, predicate);
			if (descendant is not null)
			{
				return descendant;
			}
		}

		return null;
	}

	static string FormatColor(Windows.UI.Color color) =>
		$"{color.R},{color.G},{color.B},{color.A}";
}
#endif

