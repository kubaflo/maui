#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26532, "Changing BindingContext clears the previously bound Picker selection", PlatformAffected.Android)]
public class Issue26532 : ContentPage
{
	readonly Issue26532QuestionViewModel _firstQuestion;
	readonly Issue26532QuestionViewModel _secondQuestion;
	readonly Label _originalSelectionLabel;

	public Issue26532()
	{
		_firstQuestion = new Issue26532QuestionViewModel(
			"Question 1",
			new System.Collections.ObjectModel.ObservableCollection<string>
			{
				"Answer 1",
				"Answer 2"
			});
		_secondQuestion = new Issue26532QuestionViewModel(
			"Question 2",
			new System.Collections.ObjectModel.ObservableCollection<string>());

		var promptLabel = new Label
		{
			AutomationId = "QuestionPrompt",
			FontSize = 24
		};
		promptLabel.SetBinding(Label.TextProperty, nameof(Issue26532QuestionViewModel.Prompt));

		var answerPicker = new Picker
		{
			AutomationId = "AnswerPicker",
			Title = "Select an answer"
		};
		answerPicker.SetBinding(Picker.ItemsSourceProperty, nameof(Issue26532QuestionViewModel.Answers));
		answerPicker.SetBinding(Picker.SelectedItemProperty, nameof(Issue26532QuestionViewModel.SelectedAnswer));

		_originalSelectionLabel = new Label
		{
			AutomationId = "OriginalSelectionLabel"
		};
		_originalSelectionLabel.SetBinding(Label.TextProperty, new Binding
		{
			Source = _firstQuestion,
			Path = nameof(Issue26532QuestionViewModel.SelectedAnswer),
			Mode = BindingMode.OneWay,
			StringFormat = "Original model selection: {0}",
			TargetNullValue = "none"
		});

		var pickerItemCountLabel = new Label
		{
			AutomationId = "PickerItemCountLabel"
		};
		pickerItemCountLabel.SetBinding(Label.TextProperty, new Binding
		{
			Source = answerPicker,
			Path = "ItemsSource.Count",
			StringFormat = "Picker item count: {0}"
		});

		var nextButton = new Button
		{
			AutomationId = "NextButton",
			Text = "Next"
		};
		nextButton.Clicked += OnNextClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				promptLabel,
				answerPicker,
				_originalSelectionLabel,
				pickerItemCountLabel,
				nextButton,
			}
		};

		BindingContext = _firstQuestion;
	}

	void OnNextClicked(object sender, EventArgs e)
	{
		BindingContext = _secondQuestion;
	}
}

sealed class Issue26532QuestionViewModel : System.ComponentModel.INotifyPropertyChanged
{
	string _selectedAnswer;

	public Issue26532QuestionViewModel(
		string prompt,
		System.Collections.ObjectModel.ObservableCollection<string> answers)
	{
		Prompt = prompt;
		Answers = answers;
		_selectedAnswer = null!;
	}

	public string Prompt { get; }

	public System.Collections.ObjectModel.ObservableCollection<string> Answers { get; }

	public string SelectedAnswer
	{
		get => _selectedAnswer;
		set
		{
			if (_selectedAnswer == value)
				return;

			_selectedAnswer = value;
			PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(SelectedAnswer)));
		}
	}

	public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = delegate { };
}
#endif

