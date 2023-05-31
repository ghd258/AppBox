using PixUI;

namespace AppBox.BlazorApp;

public sealed class IndexPage : SingleChildWidget
{
    private readonly Navigator _navigator;
    
    public IndexPage()
    {
        var routes = new List<Route>
        {
            new("home", s => new DemoHome()),
            new("dev", s => new AppBoxDesign.HomePage()),
        };
        _navigator = new Navigator(routes);

        Child = new RouteView(_navigator);
    }
}

public sealed class DemoHome : View
{
    public DemoHome()
    {
        Child = new Center
        {
            Child = new Text("Hello World!") { FontSize = 28 }
        };
    }
}