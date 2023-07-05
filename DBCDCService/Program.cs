using DBCDCService;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.File("C:\\Tmp\\DBCDCServiceLog\\log-.txt", rollingInterval: RollingInterval.Day).
    CreateBootstrapLogger();

IHost host = Host.CreateDefaultBuilder(args)
    //.UseSerilog()
    .UseWindowsService(config =>
    {
        config.ServiceName = "ECUS CDC Service";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();