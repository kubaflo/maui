namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29920, "Android tap event passes through overlapping containers", PlatformAffected.Android)]
public class Issue29920 : ContentPage
{
	public Issue29920()
	{
		var oracleTapMarker = -1;
		var obscuredTapCount = -1;

		var resultLabel = new Label
		{
			Text = "Oracle=-1; Obscured=-1",
			AutomationId = "Issue29920Result"
		};

		var oracleBox = new BoxView
		{
			AutomationId = "Issue29920OracleBox",
			BackgroundColor = Colors.Green,
			HeightRequest = 60
		};
		var oracleTapGesture = new TapGestureRecognizer();
		oracleTapGesture.Tapped += (_, _) =>
		{
			oracleTapMarker = 1;
			obscuredTapCount = 0;
			resultLabel.Text = $"Oracle={oracleTapMarker}; Obscured={obscuredTapCount}";
		};
		oracleBox.GestureRecognizers.Add(oracleTapGesture);

		var obscuredBox = new BoxView
		{
			AutomationId = "Issue29920ObscuredBox",
			BackgroundColor = Colors.Red,
			HeightRequest = 240
		};
		var obscuredLayer = new StackLayout
		{
			Children = { obscuredBox }
		};
		var obscuredTapGesture = new TapGestureRecognizer();
		obscuredTapGesture.Tapped += (_, _) =>
		{
			obscuredTapCount++;
			resultLabel.Text = $"Oracle={oracleTapMarker}; Obscured={obscuredTapCount}";
		};
		obscuredLayer.GestureRecognizers.Add(obscuredTapGesture);

		var coveringLayer = new StackLayout
		{
			Children =
			{
				new BoxView
				{
					BackgroundColor = Colors.Blue,
					Opacity = 0.3,
					HeightRequest = 240
				}
			}
		};

		var overlappingLayers = new Grid
		{
			HeightRequest = 240,
			Children =
			{
				obscuredLayer,
				coveringLayer
			}
		};

		Content = new StackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				oracleBox,
				overlappingLayers,
				resultLabel
			}
		};
	}
}

