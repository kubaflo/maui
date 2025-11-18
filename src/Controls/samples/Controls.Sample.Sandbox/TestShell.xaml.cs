namespace Maui.Controls.Sample;

public partial class TestShell : Shell
{
    public TestShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("subpage", typeof(TestSubPage));
    }
}
