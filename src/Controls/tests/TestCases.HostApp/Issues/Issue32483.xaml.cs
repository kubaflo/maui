using System;
using System.Linq;
using System.Text;
using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample.Issues
{
	[Issue(IssueTracker.Github, 32483, "CursorPosition not calculated correctly on iOS during TextChanged in behaviors", PlatformAffected.iOS)]
	public partial class Issue32483 : ContentPage
	{
		private bool _isUpdating;
		private int _lastValidCursorPosition = 0;

		public Issue32483()
		{
			InitializeComponent();
			TestEntry.TextChanged += OnTestEntryTextChanged;
		}

		private void OnTestEntryTextChanged(object? sender, TextChangedEventArgs e)
		{
			if (_isUpdating || sender is not Entry entry)
				return;

			string oldText = e.OldTextValue ?? string.Empty;
			string newText = e.NewTextValue ?? string.Empty;

			// Extract only digits
			string digits = new string(newText.Where(char.IsDigit).ToArray());

			// Apply CPF mask (Brazilian ID format): XXX.XXX.XXX-XX
			string masked = ApplyCpfMask(digits);

			// If no masking needed, update labels and return
			if (entry.Text == masked)
			{
				CursorLabel.Text = $"Cursor Position: {entry.CursorPosition}";
				_lastValidCursorPosition = entry.CursorPosition;
				return;
			}

			_isUpdating = true;

			// THIS IS THE KEY TEST: On iOS, CursorPosition should now return correct value
			// Before the fix, this would often return 0 or random values
			int cursorBeforeUpdate = entry.CursorPosition;

			// Verify cursor position is valid (not 0 when it shouldn't be)
			bool isValidCursor = true;
			if (newText.Length > 0 && oldText.Length > 0 && cursorBeforeUpdate == 0)
			{
				// Cursor position of 0 is suspicious when there's already text
				isValidCursor = false;
			}

			// Update text with mask
			entry.Text = masked;

			// Calculate where cursor should go (simplified logic)
			int newCursorPosition = CalculateCursorPosition(cursorBeforeUpdate, oldText.Length, masked.Length, newText.Length > oldText.Length);
			entry.CursorPosition = newCursorPosition;
			entry.SelectionLength = 0;

			// Update UI labels
			CursorLabel.Text = $"Cursor Position: Before={cursorBeforeUpdate}, After={newCursorPosition}";

			if (isValidCursor)
			{
				StatusLabel.Text = $"✓ Valid cursor: {cursorBeforeUpdate}";
				StatusLabel.TextColor = Colors.Green;
				_lastValidCursorPosition = cursorBeforeUpdate;
			}
			else
			{
				StatusLabel.Text = $"✗ Invalid cursor: {cursorBeforeUpdate} (expected non-zero)";
				StatusLabel.TextColor = Colors.Red;
			}

			_isUpdating = false;
		}

		private string ApplyCpfMask(string digits)
		{
			// Limit to 11 digits (CPF format)
			if (digits.Length > 11)
				digits = digits[..11];

			var sb = new StringBuilder();
			for (int i = 0; i < digits.Length; i++)
			{
				sb.Append(digits[i]);

				// Add dots after 3rd and 6th digit
				if ((i == 2 || i == 5) && i != digits.Length - 1)
					sb.Append('.');

				// Add dash after 9th digit
				if (i == 8 && i != digits.Length - 1)
					sb.Append('-');
			}

			return sb.ToString();
		}

		private int CalculateCursorPosition(int oldCursor, int oldLength, int newLength, bool isAdding)
		{
			// Simple cursor positioning logic
			if (isAdding)
			{
				// When adding, cursor moves forward
				return Math.Min(oldCursor + 1, newLength);
			}
			else
			{
				// When removing, cursor stays or moves back
				return Math.Max(0, oldCursor - 1);
			}
		}
	}
}
