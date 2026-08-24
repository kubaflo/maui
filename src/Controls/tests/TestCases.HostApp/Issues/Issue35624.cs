namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35624, "[iOS] SearchHandler CharacterSpacing is not applied", PlatformAffected.iOS)]
public partial class Issue35624 : Shell
{
	int _measurementSequence = -1;

	public Issue35624()
	{
		var searchHandler = new SearchHandler
		{
			CharacterSpacing = 12,
			Placeholder = "SearchHandler spacing",
			SearchBoxVisibility = SearchBoxVisibility.Expanded
		};

		var referenceLabel = new Label
		{
			AutomationId = "Issue35624ReferenceLabel",
			CharacterSpacing = 12,
			FontSize = 24,
			Text = "ABCABC"
		};

		var statusLabel = new Label
		{
			AutomationId = "Issue35624Status",
			FontSize = 18,
			Text = "Sequence=-1"
		};

		var contentPage = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 20,
				Children =
				{
					new Label
					{
						FontSize = 18,
						Text = "Type ABCABC in the search field. It should match the spaced reference below."
					},
					referenceLabel,
					statusLabel
				}
			}
		};

		Shell.SetSearchHandler(contentPage, searchHandler);
		Items.Add(new ShellContent { Content = contentPage });

		searchHandler.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName != SearchHandler.QueryProperty.PropertyName)
				return;

			_measurementSequence++;
			MeasureNativeKerning(
				searchHandler,
				referenceLabel,
				statusLabel,
				_measurementSequence);
		};
	}

	partial void MeasureNativeKerning(
		SearchHandler searchHandler,
		Label referenceLabel,
		Label statusLabel,
		int sequence);
}
