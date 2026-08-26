using Microsoft.Maui.Devices.Sensors;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36216, "Accelerometer.ReadingChanged retains subscribers after Stop", PlatformAffected.iOS)]
public class Issue36216 : NavigationPage
{
	public Issue36216() : base(new Issue36216LauncherPage())
	{
	}
}

public partial class Issue36216LauncherPage : ContentPage
{
	const int CycleCount = 5;

	readonly List<WeakReference> pageReferences = [];
	readonly List<Label> retainedPageMarkers = [];

	public Issue36216LauncherPage()
	{
		InitializeComponent();
	}

	async void OnRunCyclesClicked(object sender, EventArgs e)
	{
		RunCyclesButton.IsEnabled = false;
		CheckRetentionButton.IsEnabled = false;
		pageReferences.Clear();
		foreach (var marker in retainedPageMarkers)
		{
			RootLayout.Children.Remove(marker);
		}
		retainedPageMarkers.Clear();
		CycleStatusLabel.Text = $"Ready: 0 of {CycleCount} page cycles complete";
		RetentionDetailsLabel.Text = "Retention check not started";

		for (var index = 1; index <= CycleCount; index++)
		{
			var page = new SensorSubscriberPage(index);
			pageReferences.Add(new WeakReference(page));

			await Navigation.PushAsync(page, false);
			await Navigation.PopAsync(false);

			CycleStatusLabel.Text = $"Ready: {index} of {CycleCount} page cycles complete";
		}

		CheckRetentionButton.IsEnabled = true;
	}

	async void OnCheckRetentionClicked(object sender, EventArgs e)
	{
		CheckRetentionButton.IsEnabled = false;
		RetentionDetailsLabel.Text = "Checking retained pages";

		try
		{
			await GarbageCollectionHelper.WaitForGC(5000, pageReferences.ToArray());
		}
		catch (Exception exception) when (exception.Message == "Assertion timed out")
		{
		}

		var retainedCount = pageReferences.Count(reference => reference.IsAlive);
		for (var index = 0; index < pageReferences.Count; index++)
		{
			if (!pageReferences[index].IsAlive)
			{
				continue;
			}

			var marker = new Label
			{
				AutomationId = "Issue36216RetainedPage",
				Text = $"Retained sensor subscriber page {index + 1}"
			};
			retainedPageMarkers.Add(marker);
			RootLayout.Children.Add(marker);
		}

		RetentionDetailsLabel.Text = $"Retention check complete ({retainedCount} references inspected)";
	}

	sealed class SensorSubscriberPage : ContentPage
	{
		readonly byte[] sensorPayload = new byte[3 * 1024 * 1024];

		public SensorSubscriberPage(int index)
		{
			Title = $"Sensor page {index}";
			Content = new Label
			{
				Text = $"Sensor subscriber page {index}",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			Accelerometer.ReadingChanged += OnReadingChanged;
		}

		protected override void OnDisappearing()
		{
			try
			{
				Accelerometer.Stop();
			}
			catch (Microsoft.Maui.ApplicationModel.FeatureNotSupportedException)
			{
			}

			base.OnDisappearing();
		}

		void OnReadingChanged(object sender, AccelerometerChangedEventArgs e)
		{
			sensorPayload[0] = (byte)Math.Clamp(
				(int)Math.Abs(e.Reading.Acceleration.X),
				byte.MinValue,
				byte.MaxValue);
		}
	}
}
