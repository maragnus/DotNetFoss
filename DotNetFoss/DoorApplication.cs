using DotNetFoss.DropFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetFoss;

public class DoorApplication
{
    private IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public IDoorSession Session { get; private set; } = null!;
    public string[] Args { get; internal set; } = Array.Empty<string>();

    public static DoorBuilder CreateBuilder<TDoorApplication>(string[] args) where TDoorApplication : class, IDoorApplication
    {
        var builder = new DoorBuilder(args);
        builder.Services.AddSingleton<IDoorApplication, TDoorApplication>();
        return builder;
    }

    public DoorApplication(IServiceProvider serviceProvider, ILogger<DoorApplication> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<bool> LinkAsync()
    {
        var doorLinks = _serviceProvider.GetServices<IDropFile>();

        if (!doorLinks.Any())
        {
            _logger.LogError($"No registered {nameof(IDropFile)} services available - try calling {nameof(DoorBuilder)}.DoorHost.AddDoorLink<Door32SysLink>()");
            return false;
        }

        foreach (var doorLink in doorLinks)
        {
            var session = await doorLink.InitializeAsync();
            if (session != null)
            {
                Session = session;
                return true;
            }

            _logger.LogInformation("{DoorLink} could not initialize, skipping", doorLink.GetType().Name);
        }

        _logger.LogError($"No {nameof(IDropFile)} services were able to initialize, giving up - check BBS configuration");

        return false;
    }

    public async Task<int> RunAsync()
    {
        if (!await LinkAsync())
            return 1;

        try
        {
            var connectionTask = Session.Connection.StartAsync();

            using var scope = _serviceProvider.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<IDoorApplication>();

            await app.RunAsync();

            Session.Connection.Disconnect();
            await connectionTask;

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Door stopped due to exception");
            return 2;
        }
    }
}
