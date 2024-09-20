using PKApp.ConfigOptions;
using PKApp.DIObject;
using PKApp.Services;
using PKApp.Tools;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    //Serilog要寫入的最低等級為Information
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    //寫log到Logs資料夾的log.txt檔案中，並且以天為單位做檔案分割
    .WriteTo.File("./Logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting web host");
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddTransient<DapperContext>();
    builder.Services.AddSingleton<JwtHelpers>();
    builder.Services.AddControllers();
    builder.Services.Configure<GCSConfigOptions>(builder.Configuration);
    builder.Services.Configure<FirebaseOptions>(builder.Configuration);
    builder.Services.AddSingleton<IFilesService, FilesService>();
    builder.Services.AddSingleton<ICloudStorageService, CloudStorageService>();
    builder.Services.AddSingleton<IFirebaseService, FirebaseService>();
    builder.Services.AddSingleton<ICrypto, CryptoService>();
    builder.Services.AddSingleton<DynamicTool>();
    builder.Services.AddSingleton<IWebActivityService, WebActivityService>();
    builder.Services.AddSingleton<WebActivityTool>();
    builder.Services.AddSingleton<IVoucherSettingService, VoucherSettingService>();
    builder.Services.AddSingleton<VoucherSettingTool>();
    builder.Services.AddSingleton<IStatisticalService, StatisticalService>();
    builder.Services.AddSingleton<AppNewsStatisticalTool>();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    //controller可以使用ILogger介面來寫入log紀錄
    builder.Host.UseSerilog();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();
    app.MapFallbackToFile("index.html");
    app.UseStaticFiles();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

