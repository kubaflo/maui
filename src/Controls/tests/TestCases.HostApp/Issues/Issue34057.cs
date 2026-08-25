namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34057, "AnimationManager ObjectDisposedException when closing a child window", PlatformAffected.UWP)]
public class Issue34057 : ContentPage
{
	public Issue34057()
	{
		Title = "Issue 34057";

		var animationStateLabel = new Label
		{
			AutomationId = "Issue34057AnimationState",
			Text = "-1/NotStarted"
		};
		var animationAttemptCountLabel = new Label
		{
			AutomationId = "Issue34057AnimationAttemptCount",
			Text = "0"
		};
		var createdCountLabel = new Label
		{
			AutomationId = "Issue34057CreatedCount",
			Text = "0"
		};
		var destroyingCountLabel = new Label
		{
			AutomationId = "Issue34057DestroyingCount",
			Text = "0"
		};
		var createdWindowIdentityLabel = new Label
		{
			AutomationId = "Issue34057CreatedWindowIdentity",
			Text = "-1/None"
		};
		var closeChildWindowButton = new Button
		{
			AutomationId = "Issue34057CloseChildWindowButton",
			IsEnabled = false,
			Text = "Close child window"
		};
		var openChildWindowButton = new Button
		{
			AutomationId = "Issue34057OpenChildWindowButton",
			Text = "Open child window"
		};

		Window childWindow = null!;
		Border popup = null!;
		var createdCount = 0;
		var destroyingCount = 0;

		openChildWindowButton.Clicked += (_, _) =>
		{
			popup = new Border
			{
				AutomationId = "Issue34057Popup",
				BackgroundColor = Colors.CornflowerBlue,
				Padding = 24,
				Content = new VerticalStackLayout
				{
					Spacing = 12,
					Children =
					{
						new Label
						{
							FontSize = 22,
							Text = "Image editor save popup"
						},
						new Label
						{
							Text = "Popup animation target is visible"
						}
					}
				}
			};

			var childPage = new ContentPage
			{
				Title = "Image viewer",
				Padding = 32,
				Content = popup
			};
			childWindow = new Window(childPage)
			{
				Title = "Image viewer",
				Width = 420,
				Height = 360,
				X = 760,
				Y = 120
			};

			childWindow.Created += (sender, _) =>
			{
				createdCount++;
				closeChildWindowButton.IsEnabled = true;
				createdCountLabel.Text = createdCount.ToString();
				createdWindowIdentityLabel.Text = ReferenceEquals(sender, childWindow)
					? $"{createdCount}/ChildWindow-{createdCount}"
					: $"{createdCount}/UnexpectedWindow";
			};
			childWindow.Destroying += (_, _) =>
			{
				destroyingCount++;
				destroyingCountLabel.Text = destroyingCount.ToString();
				closeChildWindowButton.IsEnabled = false;

				Dispatcher.Dispatch(() =>
				{
					try
					{
						IAnimatable animationTarget = popup;
						AnimationExtensions.Animate<double>(
							animationTarget,
							"Issue34057PopupHide",
							value => value,
							_ => { },
							16,
							250,
							finished: (_, canceled) =>
							{
								animationStateLabel.Text = canceled ? "AnimationCanceled" : "AnimationCompleted";
								animationAttemptCountLabel.Text = "1";
							});
					}
					catch (ObjectDisposedException)
					{
						animationStateLabel.Text = "ObjectDisposedException";
						animationAttemptCountLabel.Text = "1";
					}
				});
			};

			openChildWindowButton.IsEnabled = false;
			var application = Application.Current;
			if (application is null)
			{
				animationStateLabel.Text = "ApplicationUnavailable";
				animationAttemptCountLabel.Text = "1";
				return;
			}

			application.OpenWindow(childWindow);
		};

		closeChildWindowButton.Clicked += (_, _) =>
		{
			var application = Application.Current;
			if (application is null)
			{
				animationStateLabel.Text = "ApplicationUnavailable";
				animationAttemptCountLabel.Text = "1";
				return;
			}

			application.CloseWindow(childWindow);
		};

		Content = new VerticalStackLayout
		{
			Padding = 32,
			Spacing = 20,
			Children =
			{
				new Label
				{
					FontSize = 24,
					Text = "Child window animation disposal"
				},
				new Label { Text = "Animation state:" },
				animationStateLabel,
				new Label { Text = "Animation attempt count:" },
				animationAttemptCountLabel,
				new Label { Text = "Created count:" },
				createdCountLabel,
				new Label { Text = "Destroying count:" },
				destroyingCountLabel,
				new Label { Text = "Created window identity:" },
				createdWindowIdentityLabel,
				openChildWindowButton,
				closeChildWindowButton
			}
		};
	}
}

