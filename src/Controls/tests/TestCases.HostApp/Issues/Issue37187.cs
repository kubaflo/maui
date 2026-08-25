using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37187, "Replacing Shell.FlyoutFooter leaves the previous footer active", PlatformAffected.iOS)]
public class Issue37187 : Shell
{
	readonly MeasureProbeView _removedFooter = new("Footer A", "FooterALabel");
	readonly MeasureProbeView _currentFooter = new("Footer B", "FooterBLabel");
	readonly Label _transitionStatus;
	readonly Label _replacementStatus;
	readonly Label _footerBMeasureCount;
	readonly Label _beforeMeasureCount;
	readonly Label _afterMeasureCount;
	readonly Label _completionStatus;
	bool _flyoutOpened;
	bool _flyoutClosed;

	public Issue37187()
	{
		FlyoutBehavior = FlyoutBehavior.Flyout;

		_transitionStatus = new Label
		{
			AutomationId = "TransitionStatus",
			Text = "Opened=False;Closed=False",
		};
		_replacementStatus = new Label
		{
			AutomationId = "ReplacementStatus",
			Text = "Ready=False;A=None;B=None;Current=None",
		};
		_footerBMeasureCount = new Label
		{
			AutomationId = "FooterBMeasureCount",
			Text = "-1",
		};
		_beforeMeasureCount = new Label
		{
			AutomationId = "BeforeMeasureCount",
			Text = "-1",
		};
		_afterMeasureCount = new Label
		{
			AutomationId = "AfterMeasureCount",
			Text = "-1",
		};
		_completionStatus = new Label
		{
			AutomationId = "CompletionStatus",
			Text = "Completed=False",
		};

		var prepareButton = new Button
		{
			AutomationId = "PrepareFooterButton",
			Text = "Replace footer A with B",
		};
		prepareButton.Clicked += OnPrepareFooterClicked;

		var invalidateButton = new Button
		{
			AutomationId = "InvalidateOldFooterButton",
			Text = "Invalidate removed footer A",
		};
		invalidateButton.Clicked += OnInvalidateOldFooterClicked;

		var page = new ContentPage
		{
			Title = "Main Page",
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
							AutomationId = "MainPageContent",
							FontAttributes = FontAttributes.Bold,
							FontSize = 22,
							Text = "iOS Shell stale footer callback",
						},
						new Label { Text = "1. Open the Shell flyout once, then close it." },
						new Label { Text = "2. Tap Replace footer A with B." },
						new Label { Text = "3. Tap Invalidate removed footer A." },
						new Label { Text = "Invalidating the removed footer must not measure the current footer." },
						prepareButton,
						invalidateButton,
						_transitionStatus,
						_replacementStatus,
						_footerBMeasureCount,
						_beforeMeasureCount,
						_afterMeasureCount,
						_completionStatus,
					},
				},
			},
		};

		var shellContent = new ShellContent
		{
			Title = "Main Page",
			Content = page,
		};
		var flyoutItem = new FlyoutItem
		{
			AutomationId = "MainPageFlyoutItem",
			Title = "Main Page",
		};
		flyoutItem.Items.Add(shellContent);
		Items.Add(flyoutItem);

		PropertyChanged += (_, args) =>
		{
			if (args.PropertyName != nameof(FlyoutIsPresented))
				return;

			if (FlyoutIsPresented)
				_flyoutOpened = true;
			else if (_flyoutOpened)
				_flyoutClosed = true;

			_transitionStatus.Text = $"Opened={_flyoutOpened};Closed={_flyoutClosed}";
		};
	}

	void OnPrepareFooterClicked(object sender, EventArgs e)
	{
		_removedFooter.SizeChanged += OnRemovedFooterSizeChanged;
		FlyoutFooter = _removedFooter;
	}

	void OnRemovedFooterSizeChanged(object sender, EventArgs e)
	{
		if (_removedFooter.Width <= 0 || _removedFooter.Height <= 0)
			return;

		_removedFooter.SizeChanged -= OnRemovedFooterSizeChanged;
		_currentFooter.SizeChanged += OnCurrentFooterSizeChanged;
		FlyoutFooter = _currentFooter;
	}

	void OnCurrentFooterSizeChanged(object sender, EventArgs e)
	{
		if (_currentFooter.Width <= 0 || _currentFooter.Height <= 0)
			return;

		_currentFooter.SizeChanged -= OnCurrentFooterSizeChanged;
		_currentFooter.ResetMeasureCount();
		_footerBMeasureCount.Text = _currentFooter.MeasureCount.ToString(CultureInfo.InvariantCulture);
		var currentFooterText = ReferenceEquals(FlyoutFooter, _currentFooter)
			? _currentFooter.FooterText
			: "Unexpected";
		_replacementStatus.Text =
			$"Ready=True;A={_removedFooter.FooterText};B={_currentFooter.FooterText};Current={currentFooterText}";
	}

	void OnInvalidateOldFooterClicked(object sender, EventArgs e)
	{
		var before = _currentFooter.MeasureCount;
		_beforeMeasureCount.Text = before.ToString(CultureInfo.InvariantCulture);
		_completionStatus.Text = "Completed=False";

		_removedFooter.TriggerMeasureInvalidation();

		var after = _currentFooter.MeasureCount;
		_afterMeasureCount.Text = after.ToString(CultureInfo.InvariantCulture);
		_footerBMeasureCount.Text = after.ToString(CultureInfo.InvariantCulture);
		_completionStatus.Text = "Completed=True";
	}

	sealed class MeasureProbeView : ContentView
	{
		public MeasureProbeView(string text, string labelAutomationId)
		{
			FooterText = text;
			Content = new Label
			{
				AutomationId = labelAutomationId,
				Padding = 12,
				Text = text,
			};
		}

		public string FooterText { get; }

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

