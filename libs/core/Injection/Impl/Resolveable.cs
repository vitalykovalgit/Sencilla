
namespace Sencilla.Core;

public static class StarterKit
{
    public static void Run(Action<IHostApplicationBuilder> buidler, Action<IHost>? pipeline = null)
    {
        Run([], buidler, pipeline);
    }

    public static void Run(string[] args, Action<IHostApplicationBuilder> builder, Action<IHost>? pipeline = null)
    {
        var appBuilder = Host.CreateApplicationBuilder(args);
        builder(appBuilder);

        var appPipeline = appBuilder.Build();
        pipeline?.Invoke(appPipeline);

        appPipeline.Run();
    }
}