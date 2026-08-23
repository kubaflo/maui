namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37187, "Replacing Shell.FlyoutFooter leaves the previous footer active", PlatformAffected.iOS)]
public class Issue37187 : Shell
{
	readonly MeasureProbeView _oldFooter = new("Footer A", "FooterA");
	readonly MeasureProbeView _currentFooter = new("Footer B", "FooterB");
	readonly Button _invalidateButton;
	readonly Label _footerIdentity;
	readonly Label _measureCount;
	readonly Label _beforeMeasureCount;
	readonly Label _afterMeasureCount;
	readonly Label _triggerSequence;
	readonly Label _triggerCompletion;
	readonly Label _footerStatus;

	public Issue37187()
	{
		FlyoutBehavior = FlyoutBehavior.Flyout;

		_footerIdentity = CreateStateLabel("FooterIdentity", "FooterA");
		_measureCount = CreateStateLabel("FooterBMeasureCount", "-1");
		_beforeMeasureCount = CreateStateLabel("BeforeMeasureCount", "-1");
		_afterMeasureCount = CreateStateLabel("AfterMeasureCount", "-1");
		_triggerSequence = CreateStateLabel("TriggerSequence", "-1");
		_triggerCompletion = CreateStateLabel("TriggerCompletion", "False");
		_footerStatus = CreateStateLabel("FooterStatus", "Footer A installed");
		_footerStatus.FontAttributes = FontAttributes.Bold;

		var replaceButton = new Button
		{
			AutomationId = "ReplaceFooterButton",
			Text = "Replace footer A with B"
		};
		replaceButton.Clicked += OnReplaceFooterClicked;

		_invalidateButton = new Button
		{
			AutomationId = "InvalidateOldFooterButton",
			IsEnabled = false,
			Text = "Invalidate removed footer A"
		};
		_invalidateButton.Clicked += OnInvalidateOldFooterClicked;

		var page = new ContentPage
		{
			Title = "Footer Callback",
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
						new Label { Text = "Open and close the Shell flyout once." },
						new Label { Text = "Replace footer A with B, then invalidate removed footer A." },
						replaceButton,
						_invalidateButton,
						_footerIdentity,
						_measureCount,
						_beforeMeasureCount,
						_afterMeasureCount,
						_triggerSequence,
						_triggerCompletion,
						_footerStatus
					}
				}
			}
		};

		Items.Add(new FlyoutItem
		{
			Title = "Footer Callback",
			Items = { page }
		});

		PropertyChanged += OnShellPropertyChanged;
		FlyoutFooter = _oldFooter;
	}

	static Label CreateStateLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text
		};

	void OnReplaceFooterClicked(object sender, EventArgs e)
	{
		FlyoutFooter = _currentFooter;
		_currentFooter.ResetMeasureCount();
		_footerIdentity.Text = "FooterB";
		_measureCount.Text = "0";
		_invalidateButton.IsEnabled = true;
		_footerStatus.Text = "Footer B installed";
	}

	void OnShellPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(FlyoutIsPresented) &&
			!FlyoutIsPresented &&
			ReferenceEquals(FlyoutFooter, _currentFooter))
		{
			_currentFooter.ResetMeasureCount();
			_measureCount.Text = "0";
			_footerStatus.Text = "Footer B ready";
		}
	}

	void OnInvalidateOldFooterClicked(object sender, EventArgs e)
	{
		var before = _currentFooter.MeasureCount;
		_beforeMeasureCount.Text = before.ToString();
		_triggerSequence.Text = "1";

		_oldFooter.TriggerMeasureInvalidation();

		var after = _currentFooter.MeasureCount;
		_afterMeasureCount.Text = after.ToString();
		_measureCount.Text = after.ToString();
		_triggerCompletion.Text = "Completed-1";
		_footerStatus.Text = after == before
			? "Removed footer A is detached"
			: $"Removed footer A measured footer B ({before} to {after})";
	}

	sealed class MeasureProbeView : ContentView
	{
		public MeasureProbeView(string text, string automationId)
		{
			Content = new Label
			{
				AutomationId = automationId,
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

