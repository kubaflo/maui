#if WINDOWS
using Microsoft.Maui.Platform;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WColor = Windows.UI.Color;
using WFrame = Microsoft.UI.Xaml.Controls.Frame;
using WNavigatingCancelEventArgs = Microsoft.UI.Xaml.Navigation.NavigatingCancelEventArgs;
using WPanel = Microsoft.UI.Xaml.Controls.Panel;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30203, "Unable to adjust the window background color visible when navigating", PlatformAffected.UWP)]
public class Issue30203 : NavigationPage
{
	public Issue30203()
		: base(new Issue30203PageA())
	{
	}

	sealed class Issue30203PageA : ContentPage
	{
#if WINDOWS
		const string UnsetFrameArgb = "UNSET";
#endif

		readonly Label _initialMeasurementLabel;
#if WINDOWS
		int _navigatingCallbackCount;
		string _frameArgb = UnsetFrameArgb;
#endif

		public Issue30203PageA()
		{
			Title = "Page A";
			BackgroundColor = Colors.AliceBlue;

			_initialMeasurementLabel = new Label
			{
				AutomationId = "Issue30203InitialMeasurement",
				HorizontalTextAlignment = TextAlignment.Center,
				Text = "Waiting for Page A native measurement"
			};

			var navigateButton = new Button
			{
				AutomationId = "Issue30203NavigateButton",
				Text = "Navigate to Page B"
			};
			navigateButton.Clicked += OnNavigateClicked;

			Content = CreatePageContent(
				"Page A - AliceBlue app background",
				"Issue30203PageAMarker",
				"Navigate to Page B and watch the background throughout the animated transition.",
				_initialMeasurementLabel,
				navigateButton);

#if WINDOWS
			Loaded += OnPageALoaded;
#endif
		}

		async void OnNavigateClicked(object sender, EventArgs e)
		{
			var destination = new Issue30203PageB();

#if WINDOWS
			if (Parent is NavigationPage navigationPage &&
				navigationPage.Handler?.PlatformView is WFrame frame)
			{
				void OnFrameNavigating(object eventSender, WNavigatingCancelEventArgs args)
				{
					_navigatingCallbackCount++;
					_frameArgb = frame.Background is WBrush frameBackground
						? GetBrushArgb(frameBackground)
						: "<not-solid>";
				}

				frame.Navigating += OnFrameNavigating;
				await Navigation.PushAsync(destination, true);
				frame.Navigating -= OnFrameNavigating;

				destination.UpdateMeasurement(
					_navigatingCallbackCount,
					navigationPage.Navigation.NavigationStack.Count,
					_frameArgb);
				return;
			}
#endif

			await Navigation.PushAsync(destination, true);
		}

#if WINDOWS
		void OnPageALoaded(object sender, EventArgs e)
		{
			_initialMeasurementLabel.Text = CreateMeasurement(
				_navigatingCallbackCount,
				Navigation.NavigationStack.Count,
				GetPageArgb(this),
				GetExpectedArgb(this),
				_frameArgb);
		}

		internal static string GetPageArgb(ContentPage page)
		{
			if (page.Handler?.PlatformView is WPanel panel &&
				panel.Background is WBrush pageBackground)
			{
				return GetBrushArgb(pageBackground);
			}

			return "<not-panel>";
		}

		internal static string GetExpectedArgb(ContentPage page) =>
			FormatArgb(page.BackgroundColor.ToWindowsColor());

		internal static string GetBrushArgb(WBrush brush)
		{
			if (brush is WSolidColorBrush solidColorBrush)
				return FormatArgb(solidColorBrush.Color);

			return "<not-solid>";
		}

		internal static string FormatArgb(WColor color) =>
			$"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

		internal static string CreateMeasurement(
			int callbackCount,
			int stackCount,
			string pageArgb,
			string expectedArgb,
			string frameArgb) =>
			$"callbackCount={callbackCount}; stackCount={stackCount}; pageArgb={pageArgb}; expectedArgb={expectedArgb}; frameArgb={frameArgb}";
#endif
	}

	sealed class Issue30203PageB : ContentPage
	{
		readonly Label _measurementLabel;

		public Issue30203PageB()
		{
			Title = "Page B";
			BackgroundColor = Colors.AliceBlue;

			_measurementLabel = new Label
			{
				AutomationId = "Issue30203FinalMeasurement",
				HorizontalTextAlignment = TextAlignment.Center,
				Text = "Waiting for navigation measurement"
			};

			Content = CreatePageContent(
				"Page B - AliceBlue app background",
				"Issue30203PageBMarker",
				"Both pages use the same app background; a different transition color is the reported defect.",
				_measurementLabel);
		}

#if WINDOWS
		public void UpdateMeasurement(int callbackCount, int stackCount, string frameArgb)
		{
			_measurementLabel.Text = Issue30203PageA.CreateMeasurement(
				callbackCount,
				stackCount,
				Issue30203PageA.GetPageArgb(this),
				Issue30203PageA.GetExpectedArgb(this),
				frameArgb);
		}
#endif
	}

	static Grid CreatePageContent(
		string heading,
		string markerAutomationId,
		string description,
		Label measurementLabel)
	{
		var stack = CreatePageStack(heading, markerAutomationId, description, measurementLabel);
		return CreatePageGrid(stack);
	}

	static Grid CreatePageContent(
		string heading,
		string markerAutomationId,
		string description,
		Label measurementLabel,
		Button navigateButton)
	{
		var stack = CreatePageStack(heading, markerAutomationId, description, measurementLabel);
		stack.Children.Add(navigateButton);
		return CreatePageGrid(stack);
	}

	static VerticalStackLayout CreatePageStack(
		string heading,
		string markerAutomationId,
		string description,
		Label measurementLabel) =>
		new()
		{
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			Spacing = 20,
			Children =
			{
				new Label
				{
					AutomationId = markerAutomationId,
					FontSize = 28,
					HorizontalTextAlignment = TextAlignment.Center,
					Text = heading
				},
				new Label
				{
					HorizontalTextAlignment = TextAlignment.Center,
					Text = description
				},
				measurementLabel
			}
		};

	static Grid CreatePageGrid(VerticalStackLayout stack) =>
		new()
		{
			Padding = 32,
			Children = { stack }
		};
}

