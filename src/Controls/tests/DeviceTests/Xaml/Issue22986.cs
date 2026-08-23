#if MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Xaml)]
	[Category("Issue22986")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue22986 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task XamlLoadedViewReportsAttachedParentAndIndex()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
				});
			});

			var dynamicViewHost = new ContentView
			{
				MinimumHeightRequest = 80
			};
			var detailsLabel = new Label
			{
				Text = "Waiting for a new XAML-loaded view."
			};
			var resultLabel = new Label
			{
				Text = "Visual tree change pending.",
				FontAttributes = FontAttributes.Bold
			};
			var createViewButton = new Button
			{
				Text = "Create XAML-loaded view"
			};
			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							Text = "VisualDiagnostics new-view event",
							FontSize = 24
						},
						new Label
						{
							Text = "The area below is empty before a new XAML-loaded view is created."
						},
						dynamicViewHost,
						detailsLabel,
						resultLabel,
						createViewButton
					}
				}
			};

			bool clicked = false;
			int callbackCount = 0;
			var notObserved = new object();
			object capturedParentToken = notObserved;
			int capturedIndex = int.MinValue;
			ContentView createdContentView = null;

			createViewButton.Clicked += (_, _) =>
			{
				clicked = true;
				resultLabel.Text = "Visual tree change requested.";
				detailsLabel.Text = "Creating a new XAML-loaded view.";
				createdContentView = new ContentView();
				createdContentView.LoadFromXaml(
					"""
					<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui">
					    <Label AutomationId="Issue22986AffectedView"
					           Text="New XAML-loaded view" />
					</ContentView>
					""");
				dynamicViewHost.Content = createdContentView;
			};

			void OnVisualTreeChanged(object sender, VisualTreeChangeEventArgs args)
			{
				if (args.ChangeType != VisualTreeChangeType.Add ||
					!ReferenceEquals(args.Child, createdContentView))
				{
					return;
				}

				callbackCount++;
				if (ReferenceEquals(capturedParentToken, notObserved))
				{
					capturedParentToken = args.Parent;
					capturedIndex = args.ChildIndex;
				}
			}

			bool diagnosticsInitiallyEnabled = RuntimeFeature.EnableMauiDiagnostics;
			RuntimeFeature.EnableMauiDiagnostics = true;
			VisualDiagnostics.VisualTreeChanged += OnVisualTreeChanged;

			try
			{
				Assert.True(RuntimeFeature.EnableMauiDiagnostics);

				await CreateHandlerAndAddToWindow(page, () => createViewButton.SendClicked());

				Assert.True(clicked, "The Button.Clicked handler did not run.");
				Assert.Same(createdContentView, dynamicViewHost.Content);

				var loadedLabel = Assert.IsType<Label>(createdContentView.Content);
				Assert.Equal("New XAML-loaded view", loadedLabel.Text);
				Assert.Equal("Issue22986AffectedView", loadedLabel.AutomationId);
				Assert.True(callbackCount > 0, "No Add callback identified the created ContentView.");
				Assert.NotSame(notObserved, capturedParentToken);
				Assert.NotEqual(int.MinValue, capturedIndex);

				var hostChildren = ((IVisualTreeElement)dynamicViewHost).GetVisualChildren();
				Assert.Single(hostChildren);
				Assert.Same(createdContentView, hostChildren[0]);
				Assert.Same(dynamicViewHost, ((IVisualTreeElement)createdContentView).GetVisualParent());

				string capturedParentName = capturedParentToken is null
					? "null"
					: capturedParentToken.GetType().Name;
				Assert.True(
					ReferenceEquals(dynamicViewHost, capturedParentToken) && capturedIndex == 0,
					$"VisualTreeChanged Add event for ContentView had Parent={capturedParentName} and ChildIndex={capturedIndex}; expected Parent=DynamicViewHost and ChildIndex=0.");
			}
			finally
			{
				VisualDiagnostics.VisualTreeChanged -= OnVisualTreeChanged;
				RuntimeFeature.EnableMauiDiagnostics = diagnosticsInitiallyEnabled;
			}
		}
	}
}
#endif

