using DotNetFoss;
using DotNetFoss.DropFiles;
using Microsoft.Extensions.Logging;

var builder = DoorApplication.CreateBuilder<MyDoor>(args);
builder.DoorHost.AddDoorLink<Door32Sys>();
builder.Logging.AddConsole();
return await builder.Build().RunAsync();
