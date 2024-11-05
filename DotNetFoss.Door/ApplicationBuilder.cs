using DotNetFoss.Screens.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetFoss.Door;

public class ApplicationBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection();
    
    public Widget RootWidget { get; set;  } = new Border();
    
    public Application Build()
    {
        var serviceProvider = Services.BuildServiceProvider();
        return new Application(serviceProvider, RootWidget);
    }
}