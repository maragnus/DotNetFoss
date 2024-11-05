using DotNetFoss.Screens;
using DotNetFoss.Screens.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetFoss.Door;

public class Application
{
    public ServiceProvider ServiceProvider { get; }
    public IScreenBuffer screenBuffer;

    internal Application(ServiceProvider serviceProvider, Widget rootWidget)
    {
        ServiceProvider = serviceProvider;
        Screen = new Screen { Root = rootWidget };
    }

    public static ApplicationBuilder CreateBuilder() => new ApplicationBuilder();
    
    public bool StateChanged { get; private set; }
    public Screen Screen { get; }
    public void StateHasChanged() => StateChanged = true;
    
    public async Task Run(string[] args)
    {
        var terminal = new ConsoleTerminalDriver();
        var buffer = terminal.CreateScreenBuffer();
        
        if (StateChanged)
        {
            Screen.Render(buffer);
            StateChanged = false;
        }
        
    }

}