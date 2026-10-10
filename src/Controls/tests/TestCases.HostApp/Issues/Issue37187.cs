#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37187, "Replacing Shell.FlyoutFooter leaves the previous footer active", PlatformAffected.iOS)]
public class Issue37187 : Shell
{
	public Issue37187()
	{
		FlyoutBehavior = FlyoutBehavior.Flyout;

		Items.Add(new FlyoutItem
		{
			AutomationId = "CurrentFlyoutItem",
			Title = "Stale flyout footer",
			Items =
			{
				new ShellContent
				{
					Route = "Issue37187Page",
					Title = "Stale flyout footer",
					ContentTemplate = new DataTemplate(typeof(Issue37187Page))
				}
			}
		});
	}

	public class Issue37187Page : ContentPage
	{
		readonly Label _setupStatus;
		readonly Label _footerIdentityStatus;
		readonly Label _baselineMeasurementCount;
		readonly Label _beforeMeasurementCount;
		readonly Label _afterMeasurementCount;
		readonly Label _completionGenerationStatus;
		MeasureProbeView _oldFooter = null!;
		MeasureProbeView _currentFooter = null!;
		bool _footerReady;
		bool _watchForStaleCallback;
		int _completionGeneration = -1;

		public Issue37187Page()
		{
			Title = "Stale flyout footer";

			_setupStatus = new Label
			{
				AutomationId = "SetupStatus",
				Text = "Shell flyout ready"
			};
			_footerIdentityStatus = new Label
			{
				AutomationId = "FooterIdentityStatus",
				Text = "Current footer: none"
			};
			_baselineMeasurementCount = new Label
			{
				AutomationId = "BaselineMeasurementCount",
				Text = "-1"
			};
			_beforeMeasurementCount = new Label
			{
				AutomationId = "BeforeMeasurementCount",
				Text = "-1"
			};
			_afterMeasurementCount = new Label
			{
				AutomationId = "AfterMeasurementCount",
				Text = "-1"
			};
			_completionGenerationStatus = new Label
			{
				AutomationId = "CompletionGenerationStatus",
				Text = "-1"
			};

			var replaceFooterButton = new Button
			{
				AutomationId = "ReplaceFooterButton",
				Text = "Replace footer A with B"
			};
			replaceFooterButton.Clicked += (sender, args) => ReplaceFooter();

			var invalidateOldFooterButton = new Button
			{
				AutomationId = "InvalidateOldFooterButton",
				Text = "Invalidate removed footer A"
			};
			invalidateOldFooterButton.Clicked += (sender, args) => InvalidateOldFooter();

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
						new Label { Text = "1. Open the Shell flyout once, then close it." },
						new Label { Text = "2. Tap Replace footer A with B." },
						new Label { Text = "3. Tap Invalidate removed footer A." },
						new Label { Text = "Invalidating removed footer A must not measure the current footer B." },
						replaceFooterButton,
						invalidateOldFooterButton,
						_setupStatus,
						_footerIdentityStatus,
						_baselineMeasurementCount,
						_beforeMeasurementCount,
						_afterMeasurementCount,
						_completionGenerationStatus
					}
				}
			};
		}

		void ReplaceFooter()
		{
			_footerReady = false;
			_watchForStaleCallback = false;
			_completionGeneration = -1;
			_completionGenerationStatus.Text = "-1";
			_beforeMeasurementCount.Text = "-1";
			_afterMeasurementCount.Text = "-1";
			_setupStatus.Text = "Preparing footer A";

			_oldFooter = new MeasureProbeView("Footer A", "FooterALabel", count => { });
			Shell.Current.FlyoutFooter = _oldFooter;

			Dispatcher.Dispatch(() =>
			{
				var currentFooter = new MeasureProbeView("Footer B", "FooterBLabel", OnCurrentFooterMeasured);
				_currentFooter = currentFooter;
				Shell.Current.FlyoutFooter = currentFooter;

				Dispatcher.Dispatch(() =>
				{
					currentFooter.ResetMeasureCount();
					_footerReady = true;
					_footerIdentityStatus.Text = ReferenceEquals(Shell.Current.FlyoutFooter, currentFooter)
						? "Current footer: Footer B"
						: "Current footer: unexpected";
					_baselineMeasurementCount.Text = currentFooter.MeasureCount.ToString();
					_setupStatus.Text = "Footer B ready";
				});
			});
		}

		void InvalidateOldFooter()
		{
			if (!_footerReady)
			{
				_setupStatus.Text = "Replace footer A first";
				return;
			}

			_beforeMeasurementCount.Text = _currentFooter.MeasureCount.ToString();
			_watchForStaleCallback = true;
			_oldFooter.TriggerMeasureInvalidation();

			Dispatcher.Dispatch(() =>
			{
				_watchForStaleCallback = false;
				_afterMeasurementCount.Text = _currentFooter.MeasureCount.ToString();
				_completionGeneration++;
				_completionGenerationStatus.Text = _completionGeneration.ToString();
				_setupStatus.Text = "Stale callback check complete";
			});
		}

		void OnCurrentFooterMeasured(int measureCount)
		{
			if (_watchForStaleCallback)
				_afterMeasurementCount.Text = measureCount.ToString();
		}

		sealed class MeasureProbeView : ContentView
		{
			readonly Action<int> _measured;

			public MeasureProbeView(string text, string automationId, Action<int> measured)
			{
				_measured = measured;
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
				_measured(MeasureCount);
				return base.MeasureOverride(widthConstraint, heightConstraint);
			}
		}
	}
}
#endif

