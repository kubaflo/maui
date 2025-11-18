namespace Maui.Controls.Sample;

public partial class TestMainPage : ContentPage
{
    public TestMainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Use Dispatcher to wait for layout to complete
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
        {
            // Get the content box position
            var contentBoxY = ContentBox.Bounds.Y;
            var contentBoxHeight = ContentBox.Bounds.Height;
            var pageHeight = this.Height;
            
            Console.WriteLine("=== MAIN PAGE LAYOUT INFO ===");
            Console.WriteLine($"Page Height: {pageHeight}");
            Console.WriteLine($"ContentBox Y: {contentBoxY}");
            Console.WriteLine($"ContentBox Height: {contentBoxHeight}");
            Console.WriteLine($"ContentBox Bottom: {contentBoxY + contentBoxHeight}");
            Console.WriteLine($"MainPageLabel Y: {MainPageLabel.Bounds.Y}");
            
            // On iOS, if nav bar space is incorrectly reserved, MainPageLabel.Y would be much larger
            // Expected: MainPageLabel.Y should be close to safe area top (around 47-59 for status bar + safe area)
            // Bug: MainPageLabel.Y would be much larger (adding nav bar height ~44-88 pixels)
            
            StatusLabel.Text = $"MainPageLabel Y: {MainPageLabel.Bounds.Y:F2}";
            Console.WriteLine("=============================");
        });
    }
    
    private async void OnNavigateToSubPage(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("subpage");
    }
}
