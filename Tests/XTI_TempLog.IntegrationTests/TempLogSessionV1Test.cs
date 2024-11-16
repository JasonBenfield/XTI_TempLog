using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Diagnostics;
using System.Text.Json;
using XTI_Core;
using XTI_Core.Extensions;
using XTI_Secrets.Extensions;
using XTI_TempLog.Abstractions;
using XTI_TempLog.Extensions;

namespace XTI_TempLog.IntegrationTests;

internal sealed class TempLogSessionV1Test
{
    [Test]
    public async Task ShouldWriteFileToTempLog()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSessionV1>();
        await tempLogSession.StartSession();
        var tempLog = sp.GetRequiredService<TempLogV1>();
        var files = tempLog.StartSessionFiles(DateTime.UtcNow.AddMinutes(1)).ToArray();
        Assert.That(files.Length, Is.EqualTo(1), "Should write file to temp log");
    }

    [Test]
    public async Task ShouldRenameFile()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSessionV1>();
        await tempLogSession.StartSession();
        var tempLog = sp.GetRequiredService<TempLogV1>();
        var files = tempLog.StartSessionFiles(DateTime.Now.AddMinutes(1)).ToArray();
        const string newName = "moved.txt";
        files[0].WithNewName(newName);
        var tempLogFolder = GetTempLogFolder(sp);
        var paths = Directory.GetFiles(tempLogFolder.Path());
        Assert.That(paths.Length, Is.EqualTo(1));
        Assert.That(Path.GetFileName(paths[0]), Is.EqualTo(newName), "Should rename file");
    }

    [Test]
    public async Task ShouldDeleteFile()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSessionV1>();
        await tempLogSession.StartSession();
        var tempLog = sp.GetRequiredService<TempLogV1>();
        var files = tempLog.StartSessionFiles(DateTime.UtcNow.AddMinutes(1)).ToArray();
        files[0].Delete();
        var tempLogFolder = GetTempLogFolder(sp);
        var paths = Directory.GetFiles(tempLogFolder.Path());
        Assert.That(paths.Length, Is.EqualTo(0), "Should delete file");
    }

    [Test]
    public async Task ShouldDeserializeStartSession()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSessionV1>();
        await tempLogSession.StartSession();
        var startSession = await GetSingleStartSession(sp);
        Assert.That(startSession.SessionKey?.Trim() ?? "", Is.Not.EqualTo(""), "Should deserialize start session");
    }

    [Test]
    public async Task ShouldEncryptTempLogFile()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSessionV1>();
        await tempLogSession.StartSession();
        var tempLogFolder = GetTempLogFolder(sp);
        var paths = Directory.GetFiles(tempLogFolder.Path());
        using var reader = new StreamReader(paths[0]);
        var contents = await reader.ReadToEndAsync();
        Assert.Throws<JsonException>
        (
            () => JsonSerializer.Deserialize<StartSessionModel>(contents),
            "Should encrypt temp log file"
        );
    }

    [Test]
    public async Task ShouldDecryptEventFiles()
    {
        var sp = Setup();
        var tempLog = sp.GetRequiredService<TempLogV1>();
        var eventFiles = tempLog.LogEventFiles(DateTime.Now.AddMinutes(1));
        foreach (var eventFile in eventFiles)
        {
            var contents = await eventFile.Read();
            Console.WriteLine(contents);
        }
        var startRequestFiles = tempLog.StartRequestFiles(DateTime.Now.AddMinutes(1));
        foreach (var startRequestFile in startRequestFiles)
        {
            var contents = await startRequestFile.Read();
            Console.WriteLine(contents);
        }
    }

    [Test]
    public async Task RunBenchMark()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Benchmark.RunOldVersion(1000);
        stopwatch.Stop();
        Console.WriteLine($"Time Elapsed: {stopwatch.Elapsed}");
        var sp = Setup();
        var tempLog = sp.GetRequiredService<TempLogV1>();
        await ProcessFiles(tempLog);
    }

    private static async Task ProcessFiles(TempLogV1 tempLog)
    {
        var startSessionFiles = tempLog.StartSessionFiles(DateTime.Now);
        Console.WriteLine($"Start Sessions: {startSessionFiles.Count()}");
        var startRequestFiles = tempLog.StartRequestFiles(DateTime.Now);
        Console.WriteLine($"Start Requests: {startRequestFiles.Count()}");
        var endRequestFiles = tempLog.EndRequestFiles(DateTime.Now);
        Console.WriteLine($"End Requests: {endRequestFiles.Count()}");
        var endSessionFiles = tempLog.EndSessionFiles(DateTime.Now);
        Console.WriteLine($"End Sessions: {endSessionFiles.Count()}");
        foreach (var logFile in startSessionFiles)
        {
            var contents = await logFile.Read();
            contents.WriteToConsole();
            logFile.Delete();
        }
        foreach (var logFile in startRequestFiles)
        {
            var contents = await logFile.Read();
            contents.WriteToConsole();
            logFile.Delete();
        }
        foreach (var logFile in endRequestFiles)
        {
            var contents = await logFile.Read();
            contents.WriteToConsole();
            logFile.Delete();
        }
        foreach (var logFile in endSessionFiles)
        {
            var contents = await logFile.Read();
            contents.WriteToConsole();
            logFile.Delete();
        }
    }

    private static async Task<StartSessionModel> GetSingleStartSession(IServiceProvider sp)
    {
        var tempLog = sp.GetRequiredService<TempLogV1>();
        var files = tempLog.StartSessionFiles(DateTime.Now).ToArray();
        Assert.That(files.Length, Is.EqualTo(1), "Should be one start session file");
        var serializedStartSession = await files[0].Read();
        return XtiSerializer.Deserialize<StartSessionModel>(serializedStartSession);
    }

    private static AppDataFolder GetTempLogFolder(IServiceProvider sp) =>
        sp.GetRequiredService<AppDataFolder>()
            .WithSubFolder("TempLogs");

    private IServiceProvider Setup()
    {
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddMemoryCache();
        hostBuilder.Services.AddScoped<IClock, UtcClock>();
        hostBuilder.Services.AddScoped<IAppEnvironmentContext, FakeAppEnvironmentContext>();
        hostBuilder.Services.AddSingleton<CurrentSession>();
        hostBuilder.Services.AddSingleton<XtiFolder>();
        hostBuilder.Services.AddSingleton
        (
            sp =>
                sp.GetRequiredService<XtiFolder>()
                    .AppDataFolder()
                    .WithSubFolder("OldVersion")
        );
        hostBuilder.Services.AddXtiDataProtection();
        hostBuilder.Services.AddTempLogServices();
        hostBuilder.Services.AddScoped<TempLogV1>(sp =>
        {
            var dataProtector = sp.GetDataProtector("XTI_TempLog");
            var appDataFolder = sp.GetRequiredService<AppDataFolder>();
            return new DiskTempLogV1(dataProtector, appDataFolder.WithSubFolder("TempLogs").Path());
        });
        hostBuilder.Services.AddScoped<TempLogSessionV1>();
        var host = hostBuilder.Build();
        return host.Scope();
    }
}