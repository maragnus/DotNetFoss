using DotNetFoss.DoorConnections;
using DotNetFoss.DropFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetFoss;

public static class DoorHostBuilderExtensions
{
    public static IDoorHostBuilder AddDoorLink<T>(this IDoorHostBuilder builder) where T : class, IDropFile
    {
        builder.Services.AddSingleton<IDropFile, T>();
        return builder;
    }
}

public interface IDoorHostBuilder
{
    IServiceCollection Services { get; }
}

public interface IDoorBuilder
{
    IServiceCollection Services { get; }
}

public class DoorBuilder : IDoorBuilder
{
    private string[] _args;
    public IServiceCollection Services { get; } = new ServiceCollection();
    public ILoggingBuilder Logging { get; }
    public IDoorHostBuilder DoorHost { get; }

    public DoorBuilder(string[] args)
    {
        _args = args;
        Logging = new LoggingBuilder(Services);
        DoorHost = new DoorHostBuilder(Services);
    }

    public void ConfigureServices()
    {
        Services.AddLogging();
        Services.AddSingleton<DoorConnectionFactory>();
        Services.AddSingleton<DoorApplication>();
        Services.AddSingleton((serviceProvider) => serviceProvider.GetRequiredService<DoorApplication>().Session);
        Services.AddSingleton((serviceProvider) => serviceProvider.GetRequiredService<DoorApplication>().Session.Connection);
    }

    public DoorApplication Build()
    {
        ConfigureServices();
        var serviceProvider = Services.BuildServiceProvider();
        var app = serviceProvider.GetRequiredService<DoorApplication>();
        app.Args = _args;
        return app;
    }

    private class LoggingBuilder : ILoggingBuilder
    {
        public IServiceCollection Services { get; }

        public LoggingBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }

    private class DoorHostBuilder : IDoorHostBuilder
    {
        public IServiceCollection Services { get; }

        public DoorHostBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}
