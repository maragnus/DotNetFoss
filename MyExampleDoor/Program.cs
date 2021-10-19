using DotNetFoss;
using DotNetFoss.DropFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddJsonFile("appsettings.json");
var configuration = configurationBuilder.Build();

var builder = DoorApplication.CreateBuilder<MyDoor>(args);
builder.Services.AddSingleton<IConfiguration>(configuration);
builder.DoorHost.AddDoorLink<Door32Sys>(); // DOOR32.SYS
builder.DoorHost.AddDoorLink<DoorSys>(); // DOOR.SYS
builder.Logging.AddConsole();
return await builder.Build().RunAsync();
