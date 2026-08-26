using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30778, "Setting a bound GraphicsView Shadow throws on Windows", PlatformAffected.All)]
public class Issue30778 : NavigationPage
{
	public Issue30778()
		: base(CreateMainPage())
	{
	}

	static ContentPage CreateMainPage()
	{
		var culture = CultureInfo.GetCultureInfo("en-US");
		CultureInfo.CurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;

		return new ShadowPage(culture);
	}

	sealed class ShadowPage : ContentPage
	{
		ShadowViewModel _viewModel;

		public ShadowPage(CultureInfo culture)
		{
			_viewModel = new ShadowViewModel();
			BindingContext = _viewModel;

			var optionsButton = new Button
			{
				Text = "Options",
				AutomationId = "OptionsButton"
			};
			optionsButton.Clicked += OnOptionsClicked;

			var graphicsView = new GraphicsView
			{
				AutomationId = "GraphicsViewControl",
				Margin = new Thickness(0, 100, 0, 100),
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			var graphicsViewHandlerState = new Label
			{
				Text = "AwaitingHandler",
				AutomationId = "GraphicsViewHandlerState"
			};
			graphicsView.Loaded += (_, _) =>
				graphicsViewHandlerState.Text = graphicsView.Handler is null ? "HandlerMissing" : "HandlerReady";
			graphicsView.SetBinding(GraphicsView.DrawableProperty, nameof(ShadowViewModel.Drawable));
			graphicsView.SetBinding(GraphicsView.ShadowProperty, nameof(ShadowViewModel.Shadow));
			graphicsView.SetBinding(GraphicsView.HeightRequestProperty, nameof(ShadowViewModel.HeightRequest));
			graphicsView.SetBinding(GraphicsView.WidthRequestProperty, nameof(ShadowViewModel.WidthRequest));

			var initialDiagnostics = new VerticalStackLayout
			{
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Start,
				Children =
				{
					new Label { Text = $"Culture={culture.Name}", AutomationId = "CultureState" },
					graphicsViewHandlerState,
					new Label { Text = "NotStarted", AutomationId = "InitialShadowUpdateState" },
					new Label { Text = "-1", AutomationId = "InitialCallbackCount" }
				}
			};

			var grid = new Grid
			{
				Padding = 20,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			grid.Add(optionsButton);
			grid.Add(graphicsView, 0, 1);
			grid.Add(initialDiagnostics, 0, 1);
			Content = grid;
		}

		async void OnOptionsClicked(object sender, EventArgs e)
		{
			BindingContext = _viewModel = new ShadowViewModel();
			await Navigation.PushAsync(CreateOptionsPage(_viewModel));
		}

		static ContentPage CreateOptionsPage(ShadowViewModel viewModel)
		{
			var shadowUpdateState = new Label
			{
				Text = "NotStarted",
				AutomationId = "ShadowUpdateState"
			};
			var callbackCountState = new Label
			{
				Text = "-1",
				AutomationId = "CallbackCountState"
			};
			var observedInputState = new Label
			{
				Text = "NoInput",
				AutomationId = "ObservedInputState"
			};
			var triangleSelectionState = new Label
			{
				Text = "NotSelected",
				AutomationId = "TriangleSelectionState"
			};

			var triangleOption = new RadioButton
			{
				Content = "Triangle",
				GroupName = "DrawableType",
				AutomationId = "TriangleOption"
			};
			triangleOption.CheckedChanged += (_, args) =>
			{
				if (args.Value)
				{
					viewModel.SelectTriangle();
					triangleSelectionState.Text = "Selected";
				}
			};

			var shadowInput = new Entry
			{
				AutomationId = "ShadowInputEntry",
				Keyboard = Keyboard.Text,
				Placeholder = "OffsetX,OffsetY,Radius,Opacity"
			};
			var callbackCount = -1;
			shadowInput.TextChanged += (_, args) =>
			{
				callbackCount++;
				callbackCountState.Text = callbackCount.ToString(CultureInfo.InvariantCulture);

				var input = args.NewTextValue;
				if (input is null)
				{
					observedInputState.Text = string.Empty;
					return;
				}

				observedInputState.Text = input;
				if (shadowUpdateState.Text?.StartsWith("Exception=", StringComparison.Ordinal) != true)
					UpdateShadowFromInput(viewModel, input, shadowUpdateState);
			};

			var applyButton = new Button
			{
				Text = "Apply",
				AutomationId = "ApplyButton"
			};

			var optionsPage = new ContentPage
			{
				Title = "GraphicsView Shadow Options",
				BindingContext = viewModel,
				Content = new VerticalStackLayout
				{
					Padding = 20,
					Spacing = 12,
					Children =
					{
						new Label { Text = "Drawable Type:", FontSize = 15 },
						triangleOption,
						triangleSelectionState,
						new Label { Text = "Shadow (OffsetX,OffsetY,Radius,Opacity):", FontSize = 15 },
						shadowInput,
						applyButton,
						callbackCountState,
						observedInputState,
						shadowUpdateState
					}
				}
			};
			applyButton.Clicked += async (_, _) => await optionsPage.Navigation.PopAsync();

			return optionsPage;
		}

		static void UpdateShadowFromInput(ShadowViewModel viewModel, string input, Label shadowUpdateState)
		{
			try
			{
				var parts = input.Split(',');
				viewModel.Shadow ??= new Shadow();

				if (parts.Length == 4 &&
					double.TryParse(parts[0], out var offsetX) &&
					double.TryParse(parts[1], out var offsetY) &&
					double.TryParse(parts[2], out var radius) &&
					double.TryParse(parts[3], out var opacity))
				{
					viewModel.Shadow.Offset = new Point(offsetX, offsetY);
					viewModel.Shadow.Radius = (float)radius;
					viewModel.Shadow.Opacity = (float)opacity;
					shadowUpdateState.Text = FormattableString.Invariant(
						$"Applied Offset={offsetX},{offsetY};Radius={radius};Opacity={opacity};Exception=none");
				}
				else
				{
					viewModel.Shadow.Offset = new Point(0, 0);
					viewModel.Shadow.Radius = 0;
					viewModel.Shadow.Opacity = 0;
					shadowUpdateState.Text = "Incomplete;Exception=none";
				}
			}
			catch (Exception exception) when (exception.GetType().Name == "COMException")
			{
				shadowUpdateState.Text = $"Exception={exception.GetType().Name}";
			}
		}
	}

	sealed class ShadowViewModel : INotifyPropertyChanged
	{
		Shadow _shadowValue = null!;

		public event PropertyChangedEventHandler PropertyChanged = null!;

		public IDrawable Drawable { get; private set; } = new SquareDrawable();

		public double HeightRequest => 100;

		public double WidthRequest => 100;

		public Shadow Shadow
		{
			get => _shadowValue;
			set
			{
				if (_shadowValue == value)
					return;

				_shadowValue = value;
				OnPropertyChanged();
			}
		}

		public void SelectTriangle()
		{
			Drawable = new TriangleDrawable();
			OnPropertyChanged(nameof(Drawable));
		}

		void OnPropertyChanged([CallerMemberName] string propertyName = null!) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	sealed class SquareDrawable : IDrawable
	{
		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
				return;

			var width = dirtyRect.Width * 0.8f;
			var height = dirtyRect.Height * 0.8f;
			var x = (dirtyRect.Width - width) / 2;
			var y = (dirtyRect.Height - height) / 2;

			canvas.SaveState();
			canvas.SetShadow(new SizeF(3, 3), 5, Colors.Gray);
			canvas.FillColor = Colors.Blue;
			canvas.FillRoundedRectangle(x, y, width, height, 5);
			canvas.StrokeColor = Colors.Black;
			canvas.StrokeSize = 2;
			canvas.DrawRoundedRectangle(x, y, width, height, 5);
			canvas.RestoreState();
		}
	}

	sealed class TriangleDrawable : IDrawable
	{
		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
				return;

			var width = dirtyRect.Width * 0.8f;
			var height = dirtyRect.Height * 0.8f;
			var bounds = new RectF(
				(dirtyRect.Width - width) / 2,
				(dirtyRect.Height - height) / 2,
				width,
				height);
			var path = new PathF();
			path.MoveTo(bounds.Left, bounds.Bottom);
			path.LineTo(bounds.Right, bounds.Bottom);
			path.LineTo(bounds.Center.X, bounds.Top);
			path.Close();

			canvas.SaveState();
			canvas.FillColor = Colors.Blue;
			canvas.FillPath(path);
			canvas.StrokeColor = Colors.Black;
			canvas.StrokeSize = 3;
			canvas.StrokeLineJoin = LineJoin.Round;
			canvas.DrawPath(path);
			canvas.RestoreState();
		}
	}
}

