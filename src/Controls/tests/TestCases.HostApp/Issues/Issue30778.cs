#if WINDOWS
using System.ComponentModel;
using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30778, "COMException thrown when setting Shadow via data binding on Windows", PlatformAffected.UWP)]
public class Issue30778 : NavigationPage
{
	public Issue30778() : base(new Issue30778MainPage())
	{
	}

	sealed class Issue30778MainPage : ContentPage
	{
		readonly ShadowViewModel _viewModel = new();
		Entry _resultEntry = null!;
		int _callbackSequence = -1;
		string _exceptionType = string.Empty;
		string _offset = "unset";
		string _radius = "unset";
		string _opacity = "unset";

		public Issue30778MainPage()
		{
			BindingContext = _viewModel;

			var graphicsView = new GraphicsView
			{
				AutomationId = "ShadowGraphicsView"
			};
			graphicsView.SetBinding(GraphicsView.DrawableProperty, nameof(ShadowViewModel.Drawable));
			graphicsView.SetBinding(GraphicsView.ShadowProperty, nameof(ShadowViewModel.Shadow));

			var optionsButton = new Button
			{
				AutomationId = "Options",
				Text = "Options"
			};
			optionsButton.Clicked += OnOptionsClicked;

			var grid = new Grid
			{
				Padding = 24,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				},
				RowSpacing = 14
			};
			grid.Add(new Label { Text = "GraphicsView shadow binding", FontSize = 22 });
			grid.Add(graphicsView, 0, 1);
			grid.Add(optionsButton, 0, 2);
			Content = grid;
		}

		async void OnOptionsClicked(object sender, EventArgs e)
		{
			var triangle = new RadioButton
			{
				AutomationId = "Triangle",
				Content = "Triangle",
				GroupName = "DrawableType"
			};
			triangle.CheckedChanged += OnTriangleChecked;

			var input = new Entry
			{
				AutomationId = "Input"
			};
			input.TextChanged += OnShadowInputChanged;

			_resultEntry = new Entry
			{
				AutomationId = "Result",
				IsReadOnly = true
			};
			UpdateResult();

			var options = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 14,
				Children =
				{
					new Label { Text = "Drawable Type:" },
					triangle,
					new Label { Text = "Shadow (OffsetX,OffsetY,Radius,Opacity):" },
					input,
					_resultEntry
				}
			};

			await Navigation.PushAsync(new ContentPage
			{
				Title = "GraphicsView options",
				Content = new Grid { Children = { options } }
			});
		}

		void OnTriangleChecked(object sender, CheckedChangedEventArgs e)
		{
			if (!e.Value)
				return;

			_viewModel.Drawable = new TriangleDrawable();
			UpdateResult();
		}

		void OnShadowInputChanged(object sender, TextChangedEventArgs e)
		{
			_callbackSequence++;

			try
			{
				var input = (Entry)sender;
				var parts = (input.Text ?? string.Empty).Split(',');
				double offsetX = 0;
				double offsetY = 0;
				double radius = 0;
				double opacity = 0;
				bool hasParsedValues = parts.Length == 4 &&
					double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out offsetX) &&
					double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out offsetY) &&
					double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out radius) &&
					double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out opacity);

				if (hasParsedValues)
				{
					_offset = FormattableString.Invariant($"{offsetX},{offsetY}");
					_radius = radius.ToString(CultureInfo.InvariantCulture);
					_opacity = opacity.ToString(CultureInfo.InvariantCulture);
				}

				var shadow = _viewModel.Shadow;

				if (shadow is null)
				{
					shadow = new Shadow();
					_viewModel.Shadow = shadow;
				}

				if (hasParsedValues)
				{
					shadow.Offset = new Point(offsetX, offsetY);
					shadow.Radius = (float)radius;
					shadow.Opacity = (float)opacity;
				}
				else
				{
					shadow.Offset = new Point(0, 0);
					shadow.Radius = 0;
					shadow.Opacity = 0;
				}

			}
			catch (Exception exception)
			{
				_exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
			}
			finally
			{
				UpdateResult();
			}
		}

		void UpdateResult()
		{
			_resultEntry.Text = $"Exception={_exceptionType};Sequence={_callbackSequence};Drawable={_viewModel.DrawableName};Offset={_offset};Radius={_radius};Opacity={_opacity}";
		}
	}

	sealed class ShadowViewModel : INotifyPropertyChanged
	{
		IDrawable _drawable = new SquareDrawable();
		Shadow _shadow = null!;

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		public string DrawableName => _drawable is TriangleDrawable ? "Triangle" : "Square";

		public IDrawable Drawable
		{
			get => _drawable;
			set
			{
				if (_drawable == value)
					return;

				_drawable = value;
				PropertyChanged(this, new PropertyChangedEventArgs(nameof(Drawable)));
			}
		}

		public Shadow Shadow
		{
			get => _shadow;
			set
			{
				if (_shadow == value)
					return;

				_shadow = value;
				PropertyChanged(this, new PropertyChangedEventArgs(nameof(Shadow)));
			}
		}
	}

	sealed class SquareDrawable : IDrawable
	{
		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			canvas.FillColor = Colors.CornflowerBlue;
			canvas.FillRectangle(20, 20, dirtyRect.Width - 40, dirtyRect.Height - 40);
		}
	}

	sealed class TriangleDrawable : IDrawable
	{
		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			var path = new PathF();
			path.MoveTo(dirtyRect.X + dirtyRect.Width / 2, 20);
			path.LineTo(20, dirtyRect.Bottom - 20);
			path.LineTo(dirtyRect.Right - 20, dirtyRect.Bottom - 20);
			path.Close();

			canvas.FillColor = Colors.CornflowerBlue;
			canvas.FillPath(path);
		}
	}
}
#endif

