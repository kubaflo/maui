namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37187, "Replacing Shell FlyoutFooter leaves the previous footer active", PlatformAffected.iOS)]
public class Issue37187 : Shell
{
	readonly MeasureProbeView _footerA;
	MeasureProbeView _footerB;
	readonly Label _footerIdentity;
	readonly Label _footerBBaseline;
	readonly Label _footerBMeasureBefore;
	readonly Label _footerBMeasureAfter;
	readonly Label _triggerCompletion;
	bool _footerAInstalled;

	public Issue37187()
	{
		FlyoutBehavior = FlyoutBehavior.Flyout;

		_footerA = new MeasureProbeView("Footer A", "FooterA");
		_footerB = _footerA;

		_footerIdentity = new Label
		{
			AutomationId = "FooterIdentity",
			Text = "NotInstalled"
		};
		_footerBBaseline = new Label
		{
			AutomationId = "FooterBBaseline",
			Text = "-1"
		};
		_footerBMeasureBefore = new Label
		{
			AutomationId = "FooterBMeasureBefore",
			Text = "-1"
		};
		_footerBMeasureAfter = new Label
		{
			AutomationId = "FooterBMeasureAfter",
			Text = "-1"
		};
		_triggerCompletion = new Label
		{
			AutomationId = "TriggerCompletion",
			Text = "Completion:-1"
		};

		var replaceButton = new Button
		{
			AutomationId = "ReplaceFooterButton",
			Text = "Replace footer A with B"
		};
		replaceButton.Clicked += OnReplaceFooterClicked;

		var invalidateButton = new Button
		{
			AutomationId = "InvalidateOldFooterButton",
			Text = "Invalidate removed footer A"
		};
		invalidateButton.Clicked += OnInvalidateOldFooterClicked;

		var page = new ContentPage
		{
			Title = "Footer callback",
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
							FontAttributes = FontAttributes.Bold,
							FontSize = 22,
							Text = "iOS Shell stale footer callback"
						},
						new Label { Text = "Open the Shell flyout once, close it, replace footer A, then invalidate removed footer A." },
						replaceButton,
						invalidateButton,
						_footerIdentity,
						_footerBBaseline,
						_footerBMeasureBefore,
						_footerBMeasureAfter,
						_triggerCompletion
					}
				}
			}
		};
		page.Loaded += OnPageLoaded;

		Items.Add(new FlyoutItem
		{
			Title = "Home",
			Items =
			{
				new ShellContent
				{
					Title = "Footer callback",
					Content = page
				}
			}
		});
	}

	void OnPageLoaded(object sender, EventArgs e)
	{
		if (_footerAInstalled)
			return;

		_footerAInstalled = true;
		var shell = Shell.Current ?? throw new InvalidOperationException("This scenario requires a Shell host.");
		shell.FlyoutFooter = _footerA;
		_footerIdentity.Text = "FooterA";
	}

	void OnReplaceFooterClicked(object sender, EventArgs e)
	{
		var shell = Shell.Current ?? throw new InvalidOperationException("This scenario requires a Shell host.");
		var footerB = new MeasureProbeView("Footer B", "FooterB");
		_footerB = footerB;
		shell.FlyoutFooter = footerB;

		Dispatcher.Dispatch(() =>
		{
			footerB.ResetMeasureCount();
			_footerIdentity.Text = ReferenceEquals(shell.FlyoutFooter, footerB) ? "FooterB" : "Unexpected";
			_footerBBaseline.Text = footerB.MeasureCount.ToString();
		});
	}

	void OnInvalidateOldFooterClicked(object sender, EventArgs e)
	{
		var before = _footerB.MeasureCount;
		_footerA.TriggerMeasureInvalidation();

		Dispatcher.Dispatch(() =>
		{
			_footerBMeasureBefore.Text = before.ToString();
			_footerBMeasureAfter.Text = _footerB.MeasureCount.ToString();
			_triggerCompletion.Text = "Completion:1";
		});
	}

	sealed class MeasureProbeView : ContentView
	{
		public MeasureProbeView(string text, string automationId)
		{
			AutomationId = automationId;
			Content = new Label
			{
				Padding = 12,
				Text = text
			};
		}

		public int MeasureCount { get; private set; }

		public void ResetMeasureCount() => MeasureCount = 0;

		public void TriggerMeasureInvalidation() => InvalidateMeasure();

		protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
		{
			MeasureCount++;
			return base.MeasureOverride(widthConstraint, heightConstraint);
		}
	}
}

