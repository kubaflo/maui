#if WINDOWS
using WAppBarButton = Microsoft.UI.Xaml.Controls.AppBarButton;
using WAutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34071, "Shell foreground color is not applied to ToolbarItems", PlatformAffected.UWP)]
public class Issue34071 : Shell
{
	public Issue34071()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;
		Items.Add(new ShellContent
		{
			Title = "Home",
			ContentTemplate = new DataTemplate(() => new Issue34071Page())
		});
	}
}

class Issue34071Page : ContentPage
{
	const string ToolbarItemId = "AffectedToolbarItem";
	readonly Label _nativeForegroundResult;

	public Issue34071Page()
	{
		Title = "Home";
		Shell.SetForegroundColor(this, Colors.Purple);

		ToolbarItems.Add(new ToolbarItem
		{
			AutomationId = ToolbarItemId,
			IconImageSource = "groceries.png",
			Order = ToolbarItemOrder.Primary
		});

		var expectedColor = (Color)GetValue(Shell.ForegroundColorProperty);
		var managedForeground = new Label
		{
			AutomationId = "ManagedForeground",
			Text = $"MANAGED:{expectedColor.ToRgbaHex(includeAlpha: true)}"
		};

		_nativeForegroundResult = new Label
		{
			AutomationId = "NativeForegroundResult",
			Text = "PENDING"
		};

		var checkButton = new Button
		{
			AutomationId = "CheckToolbarForegroundButton",
			Text = "Check toolbar foreground"
		};
		checkButton.Clicked += OnCheckToolbarForegroundClicked;

		Content = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 16,
			Children =
			{
				new Label
				{
					AutomationId = "Issue34071Page",
					FontSize = 20,
					Text = "Shell toolbar foreground color"
				},
				managedForeground,
				checkButton,
				_nativeForegroundResult
			}
		};

		Grid.SetRow(managedForeground, 1);
		Grid.SetRow(checkButton, 2);
		Grid.SetRow(_nativeForegroundResult, 3);
	}

	void OnCheckToolbarForegroundClicked(object sender, EventArgs e)
	{
		var root = Handler?.PlatformView as WDependencyObject;
		while (root is not null && WVisualTreeHelper.GetParent(root) is WDependencyObject parent)
			root = parent;

		if (root is null || !TryFindToolbarButton(root, out var toolbarButton))
		{
			_nativeForegroundResult.Text = "MEASURED:MISSING";
			return;
		}

		if (toolbarButton.Foreground is not WSolidColorBrush foreground)
		{
			_nativeForegroundResult.Text = $"MEASURED:{toolbarButton.Foreground?.GetType().Name ?? "NULL"}";
			return;
		}

		var actual = foreground.Color;
		var expected = (Color)GetValue(Shell.ForegroundColorProperty);
		_nativeForegroundResult.Text =
			$"MEASURED:actual={actual.R},{actual.G},{actual.B},{actual.A};expected={ToRgba(expected)}";
	}

	static bool TryFindToolbarButton(WDependencyObject element, out WAppBarButton button)
	{
		if (element is WAppBarButton candidate &&
			WAutomationProperties.GetAutomationId(candidate) == ToolbarItemId)
		{
			button = candidate;
			return true;
		}

		int childCount = WVisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < childCount; i++)
		{
			if (TryFindToolbarButton(WVisualTreeHelper.GetChild(element, i), out button))
				return true;
		}

		button = default!;
		return false;
	}

	static string ToRgba(Color color) =>
		$"{(byte)(color.Red * 255)},{(byte)(color.Green * 255)},{(byte)(color.Blue * 255)},{(byte)(color.Alpha * 255)}";
}
#endif

