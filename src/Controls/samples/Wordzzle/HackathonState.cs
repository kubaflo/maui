using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wordzzle;

public sealed class HackathonState : INotifyPropertyChanged
{
	private int _clickCount;
	private string _name = string.Empty;
	private string _validationMessage = "Enter your name and submit.";

	public event PropertyChangedEventHandler? PropertyChanged;

	public string SessionId { get; } = Guid.NewGuid().ToString("N")[..8];

	public int ClickCount
	{
		get => _clickCount;
		set
		{
			_clickCount = value;
			OnPropertyChanged();
		}
	}

	public string Name
	{
		get => _name;
		set
		{
			_name = value ?? string.Empty;
			OnPropertyChanged();
		}
	}

	public string ValidationMessage
	{
		get => _validationMessage;
		private set
		{
			_validationMessage = value;
			OnPropertyChanged();
		}
	}

	public void Submit()
	{
		ValidationMessage = string.IsNullOrWhiteSpace(Name)
			? "Please enter your name."
			: $"Hello, {Name.Trim()}!";
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
