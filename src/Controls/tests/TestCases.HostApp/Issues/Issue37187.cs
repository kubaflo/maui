namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37187, "Replacing Shell FlyoutFooter leaves the previous footer active", PlatformAffected.iOS)]
public class Issue37187 : Shell
{
	readonly MeasureProbeView _removedFooter;
	MeasureProbeView _activeFooter;
	readonly Label _beforeMeasureCount;
	readonly Label _afterMeasureCount;
	readonly Label _completionStatus;

	public Issue37187()
	{
		FlyoutBehavior = FlyoutBehavior.Flyout;

		_removedFooter = new MeasureProbeView("Footer A", "FooterA");
		_activeFooter = _removedFooter;
		FlyoutFooter = _removedFooter;

		_beforeMeasureCount = new Label
		{
			Text = "-1",
			AutomationId = "BeforeMeasureCount"
		};
		_afterMeasureCount = new Label
		{
			Text = "-1",
			AutomationId = "AfterMeasureCount"
		};
		_completionStatus = new Label
		{
			Text = "False",
			AutomationId = "CompletionStatus"
		};

		var replaceButton = new Button
		{
			Text = "Replace footer A with B",
			AutomationId = "ReplaceFooterButton"
		};
		replaceButton.Clicked += OnReplaceFooterClicked;

		var invalidateButton = new Button
		{
			Text = "Invalidate removed footer A",
			AutomationId = "InvalidateOldFooterButton"
		};
		invalidateButton.Clicked += OnInvalidateOldFooterClicked;

		var page = new ContentPage
		{
			Title = "Stale footer callback",
			Content = new ScrollView
			{
				Content = new VerticalStackLayout
				{
					Padding = 20,
					Spacing = 16,
					Children =
					{
						new Label
						{
							Text = "iOS Shell stale footer callback",
							FontAttributes = FontAttributes.Bold,
							FontSize = 22
						},
						new Label { Text = "1. Open the Shell flyout once, then close it." },
						new Label { Text = "2. Replace Footer A with Footer B." },
						new Label { Text = "3. Invalidate removed Footer A." },
						replaceButton,
						invalidateButton,
						_beforeMeasureCount,
						_afterMeasureCount,
						_completionStatus
					}
				}
			}
		};

		var item = new FlyoutItem { Title = "Issue 37187" };
		item.Items.Add(new ShellContent
		{
			Title = "Home",
			Content = page
		});
		Items.Add(item);
	}

	void OnReplaceFooterClicked(object sender, EventArgs e)
	{
		var footer = new MeasureProbeView("Footer B", "FooterB");
		_activeFooter = footer;
		FlyoutFooter = footer;
	}

	void OnInvalidateOldFooterClicked(object sender, EventArgs e)
	{
		var before = _activeFooter.MeasureCount;
		_beforeMeasureCount.Text = before.ToString();
		_afterMeasureCount.Text = "-1";
		_completionStatus.Text = "False";

		_removedFooter.TriggerMeasureInvalidation();

		Dispatcher.Dispatch(() =>
		{
			_afterMeasureCount.Text = _activeFooter.MeasureCount.ToString();
			_completionStatus.Text = "True";
		});
	}

	sealed class MeasureProbeView : ContentView
	{
		public MeasureProbeView(string text, string automationId)
		{
			AutomationId = automationId;
			Content = new Label
			{
				Text = text,
				Padding = 12
			};
		}

		public int MeasureCount { get; private set; }

		public void TriggerMeasureInvalidation() => InvalidateMeasure();

		protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
		{
			MeasureCount++;
			return base.MeasureOverride(widthConstraint, heightConstraint);
		}
	}
}
