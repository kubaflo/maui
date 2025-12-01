using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
	private double _sliderMin = 10;
	private double _sliderMax = 100;
	private double _sliderValue = 50;
	private double _stepperMin = 10;
	private double _stepperMax = 100;
	private double _stepperValue = 50;

	public double SliderMin
	{
		get => _sliderMin;
		set { _sliderMin = value; OnPropertyChanged(); }
	}

	public double SliderMax
	{
		get => _sliderMax;
		set { _sliderMax = value; OnPropertyChanged(); }
	}

	public double SliderValue
	{
		get => _sliderValue;
		set { _sliderValue = value; OnPropertyChanged(); }
	}

	public double StepperMin
	{
		get => _stepperMin;
		set { _stepperMin = value; OnPropertyChanged(); }
	}

	public double StepperMax
	{
		get => _stepperMax;
		set { _stepperMax = value; OnPropertyChanged(); }
	}

	public double StepperValue
	{
		get => _stepperValue;
		set { _stepperValue = value; OnPropertyChanged(); }
	}

	public MainPage()
	{
		Console.WriteLine("=== SANDBOX: MainPage Constructor START ===");
		BindingContext = this;
		InitializeComponent();
		Console.WriteLine($"=== SANDBOX: After InitializeComponent - Slider Value: {SliderValue}, Stepper Value: {StepperValue} ===");
		ValidateInitialState();
	}

	private void ValidateInitialState()
	{
		Console.WriteLine("=== SANDBOX: Validating Initial State ===");
		bool sliderValid = Math.Abs(SliderValue - 50) < 0.01;
		bool stepperValid = Math.Abs(StepperValue - 50) < 0.01;
		
		Console.WriteLine($"Slider - Min: {SliderMin}, Max: {SliderMax}, Value: {SliderValue} (Expected: 50, Valid: {sliderValid})");
		Console.WriteLine($"Stepper - Min: {StepperMin}, Max: {StepperMax}, Value: {StepperValue} (Expected: 50, Valid: {stepperValid})");
		
		if (sliderValid && stepperValid)
		{
			ValidationStatusLabel.Text = "✅ All tests PASSED - Values correctly preserved!";
			ValidationStatusLabel.TextColor = Colors.Green;
			Console.WriteLine("=== SANDBOX: ✅ VALIDATION PASSED ===");
		}
		else
		{
			ValidationStatusLabel.Text = $"❌ FAILED - Slider: {SliderValue} (expected 50), Stepper: {StepperValue} (expected 50)";
			ValidationStatusLabel.TextColor = Colors.Red;
			Console.WriteLine("=== SANDBOX: ❌ VALIDATION FAILED ===");
		}
	}

	private void OnTestValueMinMax(object sender, EventArgs e)
	{
		Console.WriteLine("=== SANDBOX: Testing Order - Value → Min → Max ===");
		var slider = new Slider();
		slider.Value = 50;
		slider.Minimum = 10;
		slider.Maximum = 100;
		
		bool passed = Math.Abs(slider.Value - 50) < 0.01;
		Console.WriteLine($"Result: Value={slider.Value} (Expected: 50, Passed: {passed})");
		
		ValidationStatusLabel.Text = passed ? "✅ Value→Min→Max: PASSED" : $"❌ Value→Min→Max: FAILED (Got {slider.Value})";
		ValidationStatusLabel.TextColor = passed ? Colors.Green : Colors.Red;
	}

	private void OnTestMinValueMax(object sender, EventArgs e)
	{
		Console.WriteLine("=== SANDBOX: Testing Order - Min → Value → Max ===");
		var slider = new Slider();
		slider.Minimum = 10;
		slider.Value = 50;
		slider.Maximum = 100;
		
		bool passed = Math.Abs(slider.Value - 50) < 0.01;
		Console.WriteLine($"Result: Value={slider.Value} (Expected: 50, Passed: {passed})");
		
		ValidationStatusLabel.Text = passed ? "✅ Min→Value→Max: PASSED" : $"❌ Min→Value→Max: FAILED (Got {slider.Value})";
		ValidationStatusLabel.TextColor = passed ? Colors.Green : Colors.Red;
	}

	private void OnTestMaxValueMin(object sender, EventArgs e)
	{
		Console.WriteLine("=== SANDBOX: Testing Order - Max → Value → Min ===");
		var slider = new Slider();
		slider.Maximum = 100;
		slider.Value = 50;
		slider.Minimum = 10;
		
		bool passed = Math.Abs(slider.Value - 50) < 0.01;
		Console.WriteLine($"Result: Value={slider.Value} (Expected: 50, Passed: {passed})");
		
		ValidationStatusLabel.Text = passed ? "✅ Max→Value→Min: PASSED" : $"❌ Max→Value→Min: FAILED (Got {slider.Value})";
		ValidationStatusLabel.TextColor = passed ? Colors.Green : Colors.Red;
	}

	private void OnShrinkRange(object sender, EventArgs e)
	{
		Console.WriteLine("=== SANDBOX: Shrinking range to 0-10 ===");
		Console.WriteLine($"Before: Min={DynamicSlider.Minimum}, Max={DynamicSlider.Maximum}, Value={DynamicSlider.Value}");
		
		DynamicSlider.Minimum = 0;
		DynamicSlider.Maximum = 10;
		
		Console.WriteLine($"After: Min={DynamicSlider.Minimum}, Max={DynamicSlider.Maximum}, Value={DynamicSlider.Value}");
		Console.WriteLine("Value should be clamped to 10, but original value (50) should be remembered");
	}

	private void OnExpandRange(object sender, EventArgs e)
	{
		Console.WriteLine("=== SANDBOX: Expanding range back to 0-100 ===");
		Console.WriteLine($"Before: Min={DynamicSlider.Minimum}, Max={DynamicSlider.Maximum}, Value={DynamicSlider.Value}");
		
		DynamicSlider.Maximum = 100;
		
		Console.WriteLine($"After: Min={DynamicSlider.Minimum}, Max={DynamicSlider.Maximum}, Value={DynamicSlider.Value}");
		
		bool passed = Math.Abs(DynamicSlider.Value - 50) < 0.01;
		Console.WriteLine($"Expected value to restore to 50: {passed}");
		
		ValidationStatusLabel.Text = passed ? "✅ Value restored to 50!" : $"❌ Value NOT restored (Got {DynamicSlider.Value})";
		ValidationStatusLabel.TextColor = passed ? Colors.Green : Colors.Red;
	}

	public new event PropertyChangedEventHandler? PropertyChanged;

	protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}