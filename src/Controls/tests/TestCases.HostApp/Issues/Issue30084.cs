namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30084, "InputView with TextTransform Uppercase triggers TextChanged twice per character", PlatformAffected.UWP)]
public class Issue30084 : ContentPage
{
	public Issue30084()
	{
		var entryTraceLabel = CreateTraceLabel("EntryEventTrace");
		var editorTraceLabel = CreateTraceLabel("EditorEventTrace");
		var searchBarTraceLabel = CreateTraceLabel("SearchBarEventTrace");

		var entryTrace = string.Empty;
		var editorTrace = string.Empty;
		var searchBarTrace = string.Empty;

		var uppercaseEntry = new Entry
		{
			AutomationId = "UppercaseEntry",
			Placeholder = "Type d",
			TextTransform = TextTransform.Uppercase
		};
		uppercaseEntry.TextChanged += (_, e) =>
			entryTrace = RecordTransition(entryTraceLabel, entryTrace, e);

		var uppercaseEditor = new Editor
		{
			AutomationId = "UppercaseEditor",
			Placeholder = "Type e",
			TextTransform = TextTransform.Uppercase
		};
		uppercaseEditor.TextChanged += (_, e) =>
			editorTrace = RecordTransition(editorTraceLabel, editorTrace, e);

		var uppercaseSearchBar = new SearchBar
		{
			AutomationId = "UppercaseSearchBar",
			Placeholder = "Type f",
			TextTransform = TextTransform.Uppercase
		};
		uppercaseSearchBar.TextChanged += (_, e) =>
			searchBarTrace = RecordTransition(searchBarTraceLabel, searchBarTrace, e);

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 10,
				Children =
				{
					new Label
					{
						Text = "TextTransform Uppercase TextChanged events",
						FontSize = 20,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "Type one lowercase character. A single input should not raise both a raw lowercase event and a transformed uppercase event.",
						LineBreakMode = LineBreakMode.WordWrap
					},
					new Label { Text = "Entry", FontAttributes = FontAttributes.Bold },
					uppercaseEntry,
					entryTraceLabel,
					new Label { Text = "Editor", FontAttributes = FontAttributes.Bold },
					uppercaseEditor,
					editorTraceLabel,
					new Label { Text = "SearchBar", FontAttributes = FontAttributes.Bold },
					uppercaseSearchBar,
					searchBarTraceLabel
				}
			}
		};
	}

	static Label CreateTraceLabel(string automationId) =>
		new()
		{
			AutomationId = automationId,
			Text = "NO EVENTS",
			LineBreakMode = LineBreakMode.WordWrap
		};

	static string RecordTransition(Label traceLabel, string trace, TextChangedEventArgs e)
	{
		var oldText = string.IsNullOrEmpty(e.OldTextValue) ? "<empty>" : e.OldTextValue;
		var newText = string.IsNullOrEmpty(e.NewTextValue) ? "<empty>" : e.NewTextValue;
		var transition = $"{oldText} -> {newText}";
		var updatedTrace = string.IsNullOrEmpty(trace) ? transition : $"{trace}; {transition}";

		traceLabel.Text = updatedTrace;
		return updatedTrace;
	}
}

