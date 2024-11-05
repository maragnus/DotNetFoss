namespace DotNetFoss.Driver;

public sealed class DoorApplication<TApplication> where TApplication : IDoorApplication, new()
{
    public static DoorApplicationBuilder<TApplication> CreateBuilder(string[] args)
    {
        return new(new DoorApplicationOptions() { Args = args });
    }
}

public class DoorApplicationOptions
{
    public string[] Args { get; init; } = [];

}

public class DoorApplicationBuilder<TApplication>(DoorApplicationOptions options) where TApplication : IDoorApplication, new()
{
    public IDoorApplication Build()
    {
        return new TApplication();
    }
}

public interface IDoorApplication
{
}