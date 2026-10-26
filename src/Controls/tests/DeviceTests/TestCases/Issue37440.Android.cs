#if ANDROID
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AView = Android.Views.View;
using AViewStates = Android.Views.ViewStates;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37440")]
	public class Issue37440 : ControlsHandlerTestBase
	{
		const double ExpectedMinimumHeight = 50;
		const double HeightTolerance = 1;

		[Fact]
		public async Task EmptyAutoSizingEditorStartsAtMinimumHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Editor, EditorHandler>();
				});
			});

			var calibrationEditor = CreateEditor();
			calibrationEditor.HeightRequest = ExpectedMinimumHeight;
			var calibrationHierarchy = CreateHierarchy(calibrationEditor);
			var calibrationHeight = await GetNativeHeightAfterAttachment(calibrationHierarchy.ScrollView, calibrationEditor);

			Assert.True(
				Math.Abs(calibrationHeight - ExpectedMinimumHeight) <= HeightTolerance,
				$"Calibration Editor native height should be {ExpectedMinimumHeight:0.##} DIP, but was {calibrationHeight:0.##} DIP.");

			var reportedEditor = CreateEditor();
			var reportedHierarchy = CreateHierarchy(reportedEditor);
			var reportedHeight = await GetNativeHeightAfterAttachment(reportedHierarchy.ScrollView, reportedEditor);

			Assert.True(string.IsNullOrEmpty(reportedEditor.Text));
			Assert.Equal(ExpectedMinimumHeight, reportedEditor.MinimumHeightRequest);
			Assert.Equal(150, reportedEditor.MaximumHeightRequest);
			Assert.Equal(EditorAutoSizeOption.TextChanges, reportedEditor.AutoSize);
			Assert.Equal(Colors.Yellow, Assert.IsType<SolidColorBrush>(reportedEditor.Background).Color);
			Assert.Equal(Colors.Black, reportedEditor.TextColor);
			Assert.Equal(200, reportedHierarchy.OuterBorder.HeightRequest);
			Assert.Equal(Colors.Green, Assert.IsType<SolidColorBrush>(reportedHierarchy.OuterBorder.Background).Color);
			Assert.Equal(150, reportedHierarchy.InnerBorder.HeightRequest);
			Assert.Equal(Colors.Blue, Assert.IsType<SolidColorBrush>(reportedHierarchy.InnerBorder.Background).Color);
			Assert.Same(reportedEditor, reportedHierarchy.InnerBorder.Content);
			Assert.Same(reportedHierarchy.InnerBorder, reportedHierarchy.OuterBorder.Content);
			Assert.Single(reportedHierarchy.StackLayout.Children);
			Assert.Same(reportedHierarchy.OuterBorder, reportedHierarchy.StackLayout.Children[0]);
			Assert.Same(reportedHierarchy.StackLayout, reportedHierarchy.ScrollView.Content);

			Assert.True(
				Math.Abs(reportedHeight - reportedEditor.MinimumHeightRequest) <= HeightTolerance,
				$"Issue 37440: empty Editor native height should start at its 50 DIP minimum. Expected {reportedEditor.MinimumHeightRequest:0.##} DIP, but was {reportedHeight:0.##} DIP.");
		}

		async Task<double> GetNativeHeightAfterAttachment(ScrollView hierarchy, Editor editor)
		{
			var layoutObserved = new TaskCompletionSource<int>();
			AView nativeEditor = null;
			var observedHeight = -1;
			var displayDensity = -1f;

			editor.HandlerChanged += OnHandlerChanged;

			try
			{
				await CreateHandlerAndAddToWindow(hierarchy, async () =>
				{
					var callbackHeight = await layoutObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

					Assert.NotNull(nativeEditor);
					Assert.NotNull(editor.Handler);
					Assert.IsType<EditorHandler>(editor.Handler);
					Assert.IsType<MauiAppCompatEditText>(nativeEditor);
					Assert.Same(editor, editor.Handler.VirtualView);
					Assert.Same(nativeEditor, editor.Handler.PlatformView);
					Assert.True(nativeEditor.IsAttachedToWindow);
					Assert.Equal(AViewStates.Visible, nativeEditor.Visibility);
					Assert.True(nativeEditor.MeasuredWidth > 0);
					Assert.True(nativeEditor.MeasuredHeight > 0);
					Assert.True(nativeEditor.Width > 0);
					Assert.True(nativeEditor.Height > 0);
					Assert.True(callbackHeight > 0);

					displayDensity = nativeEditor.Context.Resources.DisplayMetrics.Density;
					Assert.True(displayDensity > 0);
					observedHeight = nativeEditor.MeasuredHeight;
				});
			}
			finally
			{
				editor.HandlerChanged -= OnHandlerChanged;
				if (nativeEditor != null)
					nativeEditor.LayoutChange -= OnLayoutChanged;
			}

			return observedHeight / displayDensity;

			void OnHandlerChanged(object sender, EventArgs e)
			{
				if (editor.Handler?.PlatformView is not AView platformView)
					return;

				nativeEditor = platformView;
				nativeEditor.LayoutChange += OnLayoutChanged;
			}

			void OnLayoutChanged(object sender, AView.LayoutChangeEventArgs e)
			{
				if (sender is not AView platformView ||
					!platformView.IsAttachedToWindow ||
					platformView.Visibility != AViewStates.Visible ||
					platformView.MeasuredWidth <= 0 ||
					platformView.MeasuredHeight <= 0)
				{
					return;
				}

				layoutObserved.TrySetResult(platformView.MeasuredHeight);
			}
		}
		static Editor CreateEditor() =>
			new Editor
			{
				MinimumHeightRequest = ExpectedMinimumHeight,
				MaximumHeightRequest = 150,
				AutoSize = EditorAutoSizeOption.TextChanges,
				Background = Colors.Yellow,
				TextColor = Colors.Black,
			};

		static (
			ScrollView ScrollView,
			VerticalStackLayout StackLayout,
			Border OuterBorder,
			Border InnerBorder) CreateHierarchy(Editor editor)
		{
			var innerBorder = new Border
			{
				HeightRequest = 150,
				Background = Colors.Blue,
				Content = editor,
			};
			var outerBorder = new Border
			{
				HeightRequest = 200,
				Background = Colors.Green,
				Content = innerBorder,
			};
			var stackLayout = new VerticalStackLayout
			{
				Padding = 16,
				Spacing = 12,
				Children =
				{
					outerBorder,
				},
			};
			var scrollView = new ScrollView
			{
				Content = stackLayout,
			};

			return (scrollView, stackLayout, outerBorder, innerBorder);
		}
	}
}
#endif

