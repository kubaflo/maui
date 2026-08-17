namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, "33037Repro", "iOS Large Title display disappears", PlatformAffected.iOS)]
public class Issue33037Repro : NavigationPage
{
	public Issue33037Repro() : base(new LargeTitlePage())
	{
	}

	sealed class LargeTitlePage : ContentPage
	{
		readonly Label _resultStatus;
		bool _scrollTriggerRecorded;

		public LargeTitlePage()
		{
			Title = "Large Title Test";

			Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetLargeTitleDisplay(
				this,
				Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.LargeTitleDisplayMode.Always);

			_resultStatus = new Label
			{
				AutomationId = "ResultStatus",
				BackgroundColor = Colors.White,
				Padding = 12,
				Text = "NO BUG: waiting for the scroll trigger",
				TextColor = Colors.Black,
				ZIndex = 10
			};

			var scrollContent = new VerticalStackLayout
			{
				Padding = new Thickness(20, 12),
				Spacing = 14,
				Children =
				{
					_resultStatus,
					new Label
					{
						Text = "Large Title Test Page",
						FontSize = 18
					}
				}
			};

			for (int i = 1; i <= 40; i++)
			{
				scrollContent.Children.Add(new Label
				{
					Text = $"Item {i} - Scroll to collapse the large title",
					FontSize = 16,
					Padding = new Thickness(0, 8)
				});
			}

			var scrollView = new ScrollView
			{
				AutomationId = "TestScrollView",
				Content = scrollContent
			};
			scrollView.Scrolled += OnScrollViewScrolled;
			Content = scrollView;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			if (Parent is NavigationPage navigationPage)
			{
				Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.NavigationPage.SetPrefersLargeTitles(
					navigationPage,
					true);
			}
		}

		void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
		{
			_resultStatus.TranslationY = e.ScrollY;

			if (_scrollTriggerRecorded || e.ScrollY < 80)
				return;

			_scrollTriggerRecorded = true;
			_resultStatus.Text = "SCROLLED PAST 80";
		}
	}
}
