using System.Text;
using DotNetFoss.Door;
using DotNetFoss.Screens.Widgets;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Mvc.RazorPages;

Console.OutputEncoding = Encoding.UTF8;

var builder = Application.CreateBuilder();

builder.RootWidget = new Router()
{
    
};

await builder.Build().Run(args);

class MainMenu : Page
{
    private void OnNewCharacter()
    {
        Get<Screen>().Root = new NewCharacter();
    }
    
    Widget Render()
    {
        return new Columns()
        {
            Children =
            [
                new Stack
                {
                    AlignItems = AlignItems.Center,
                    Children = [
                        new MenuItem("New Character", OnNewCharacter),
                        new MenuItem("Continue", OnContinue),
                        new MenuItem("Settings", OnSettings),
                        new MenuItem("Quit", OnQuit),
                    ]
                }
            ]
        };
    }
}