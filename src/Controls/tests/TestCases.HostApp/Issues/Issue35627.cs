namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35627, "Loaded does not fire after disconnecting a gesture control and re-entering a TabbedPage tab", PlatformAffected.iOS)]
public class Issue35627 : ContentPage
{
	public Issue35627()
	{
		var openTabsButton = new Button
		{
			AutomationId = "OpenLifecycleTabs",
			Text = "Open lifecycle tabs"
		};

		openTabsButton.Clicked += (sender, args) =>
		{
			openTabsButton.IsEnabled = false;

			var loadedCount = 0;
			var unloadedCount = 0;
			var checkSequence = -1;

			var eventStateLabel = new Label
			{
				AutomationId = "LifecycleEvents",
				Text = "Loaded=0; Unloaded=0",
				HorizontalOptions = LayoutOptions.Center
			};
			var identityLabel = new Label
			{
				AutomationId = "ControlIdentity",
				HorizontalOptions = LayoutOptions.Center
			};
			var recognizerCountLabel = new Label
			{
				AutomationId = "RecognizerCount",
				HorizontalOptions = LayoutOptions.Center
			};
			var checkSequenceLabel = new Label
			{
				AutomationId = "CheckSequence",
				Text = "Check=-1",
				HorizontalOptions = LayoutOptions.Center
			};
			var otherReadyLabel = new Label
			{
				AutomationId = "OtherReady",
				Text = "OtherReady; Loaded=0; Unloaded=0",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			var gestureControl = new ContentView
			{
				AutomationId = "GestureControl",
				BackgroundColor = Colors.LightBlue,
				HeightRequest = 120,
				Content = new Label
				{
					Text = "Gesture control",
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}
			};

			gestureControl.GestureRecognizers.Add(new TapGestureRecognizer());
			var identityToken = $"Instance={gestureControl.GetHashCode()}";
			identityLabel.Text = identityToken;
			recognizerCountLabel.Text = $"Recognizers={gestureControl.GestureRecognizers.Count}";

			gestureControl.Loaded += (loadedSender, loadedArgs) =>
			{
				loadedCount++;
				eventStateLabel.Text = $"Loaded={loadedCount}; Unloaded={unloadedCount}";
			};
			gestureControl.Unloaded += (unloadedSender, unloadedArgs) =>
			{
				unloadedCount++;
				eventStateLabel.Text = $"Loaded={loadedCount}; Unloaded={unloadedCount}";
				otherReadyLabel.Text = $"OtherReady; Loaded={loadedCount}; Unloaded={unloadedCount}";

				if (unloadedSender is VisualElement visualElement)
					visualElement.Handler?.DisconnectHandler();
			};

			var checkButton = new Button
			{
				AutomationId = "CheckLifecycle",
				Text = "Check lifecycle"
			};
			checkButton.Clicked += (checkSender, checkArgs) =>
			{
				checkSequence++;
				checkSequenceLabel.Text = $"Check={checkSequence}";
				identityLabel.Text = identityToken;
				recognizerCountLabel.Text = $"Recognizers={gestureControl.GestureRecognizers.Count}";
				eventStateLabel.Text = $"Loaded={loadedCount}; Unloaded={unloadedCount}";
			};

			var lifecyclePage = new ContentPage
			{
				Title = "Lifecycle",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							Text = "The blue gesture control must load again after tab re-entry.",
							HorizontalOptions = LayoutOptions.Center
						},
						identityLabel,
						recognizerCountLabel,
						eventStateLabel,
						gestureControl,
						checkSequenceLabel,
						checkButton
					}
				}
			};
			var otherPage = new ContentPage
			{
				Title = "Other",
				Content = otherReadyLabel
			};
			var tabs = new TabbedPage();
			tabs.Children.Add(lifecyclePage);
			tabs.Children.Add(otherPage);

			_ = Navigation.PushModalAsync(tabs);
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "Issue 35627: TabbedPage lifecycle",
					FontSize = 22,
					HorizontalOptions = LayoutOptions.Center
				},
				openTabsButton
			}
		};
	}
}

