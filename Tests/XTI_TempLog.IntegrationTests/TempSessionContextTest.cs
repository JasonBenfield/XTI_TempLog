using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Text.Json;
using XTI_Core;
using XTI_Core.Extensions;
using XTI_Secrets.Extensions;
using XTI_TempLog.Abstractions;
using XTI_TempLog.Extensions;

namespace XTI_TempLog.IntegrationTests;

internal sealed class TempSessionContextTest
{
    [Test]
    public async Task ShouldWriteFileToTempLog()
    {
        var sp = setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        var tempLog = sp.GetRequiredService<TempLog>();
        var files = tempLog.StartSessionFiles(DateTime.UtcNow.AddMinutes(1)).ToArray();
        Assert.That(files.Length, Is.EqualTo(1), "Should write file to temp log");
    }

    [Test]
    public async Task ShouldRenameFile()
    {
        var sp = setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        var tempLog = sp.GetRequiredService<TempLog>();
        var files = tempLog.StartSessionFiles(DateTime.Now.AddMinutes(1)).ToArray();
        const string newName = "moved.txt";
        files[0].WithNewName(newName);
        var tempLogFolder = getTempLogFolder(sp);
        var paths = Directory.GetFiles(tempLogFolder.Path());
        Assert.That(paths.Length, Is.EqualTo(1));
        Assert.That(Path.GetFileName(paths[0]), Is.EqualTo(newName), "Should rename file");
    }

    [Test]
    public async Task ShouldDeleteFile()
    {
        var sp = setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        var tempLog = sp.GetRequiredService<TempLog>();
        var files = tempLog.StartSessionFiles(DateTime.UtcNow.AddMinutes(1)).ToArray();
        files[0].Delete();
        var tempLogFolder = getTempLogFolder(sp);
        var paths = Directory.GetFiles(tempLogFolder.Path());
        Assert.That(paths.Length, Is.EqualTo(0), "Should delete file");
    }

    [Test]
    public async Task ShouldDeserializeStartSession()
    {
        var sp = setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        var startSession = await getSingleStartSession(sp);
        Assert.That(startSession.SessionKey?.Trim() ?? "", Is.Not.EqualTo(""), "Should deserialize start session");
    }

    [Test]
    public async Task ShouldEncryptTempLogFile()
    {
        var sp = setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        var tempLogFolder = getTempLogFolder(sp);
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
        var sp = setup();
        var tempLog = sp.GetRequiredService<TempLog>();
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

    private static async Task<StartSessionModel> getSingleStartSession(IServiceProvider sp)
    {
        var tempLog = sp.GetRequiredService<TempLog>();
        var files = tempLog.StartSessionFiles(DateTime.Now).ToArray();
        Assert.That(files.Length, Is.EqualTo(1), "Should be one start session file");
        var serializedStartSession = await files[0].Read();
        return XtiSerializer.Deserialize<StartSessionModel>(serializedStartSession);
    }

    private static AppDataFolder getTempLogFolder(IServiceProvider sp)
    {
        return sp.GetRequiredService<AppDataFolder>().WithSubFolder("TempLogs");
    }

    private IServiceProvider setup()
    {
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddMemoryCache();
        hostBuilder.Services.AddScoped<IClock, UtcClock>();
        hostBuilder.Services.AddScoped<IAppEnvironmentContext, TestAppEnvironmentContext>();
        hostBuilder.Services.AddSingleton<CurrentSession>();
        hostBuilder.Services.AddSingleton<XtiFolder>();
        hostBuilder.Services.AddSingleton
        (
            sp =>
                sp.GetRequiredService<XtiFolder>()
                    .SharedAppDataFolder()
                    .WithSubFolder("TestTempLog")
        );
        hostBuilder.Services.AddXtiDataProtection();
        hostBuilder.Services.AddTempLogServices();
        var host = hostBuilder.Build();
        deleteTempLogFolder(host.GetRequiredService<AppDataFolder>());
        return host.Scope();
    }

    private void deleteTempLogFolder(AppDataFolder appDataFolder)
    {
        var dir = appDataFolder.Path();
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.GetFiles(dir))
            {
                File.Delete(file);
            }
            foreach (var childDir in Directory.GetDirectories(dir))
            {
                foreach (var childFile in Directory.GetFiles(childDir))
                {
                    File.Delete(childFile);
                }
                Directory.Delete(childDir);
            }
            Directory.Delete(dir);
        }
    }

    private sealed class TestAppEnvironmentContext : IAppEnvironmentContext
    {
        private readonly AppEnvironment appEnv;

        public TestAppEnvironmentContext()
        {
            appEnv = new AppEnvironment
            (
                "test.user", "my-computer", "10.1.0.0", "Windows 10", "WebApp"
            );
        }

        public Task<AppEnvironment> Value() => Task.FromResult(appEnv);
    }
}