using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Diagnostics;
using XTI_Core;
using XTI_Core.Extensions;
using XTI_Secrets.Extensions;
using XTI_TempLog.Abstractions;
using XTI_TempLog.Extensions;

namespace XTI_TempLog.IntegrationTests;

internal sealed class TempLogSessionTest
{
    [Test]
    public async Task ShouldWriteFileToTempLog()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        var files = await WriteLogFiles(sp);
        Assert.That(files.Length, Is.EqualTo(1), "Should write file to temp log");
    }

    [Test]
    public async Task ShouldDecryptFile()
    {
        var sp = Setup();
        var dataProtector = sp.GetDataProtector("XTI_TempLog");
        var diskTempLog = new DiskTempLog(dataProtector, "C:\\XTI\\AppData\\Development\\TempLogTest");
        var files = diskTempLog.Files(DateTimeOffset.Now);
        var sessionDetails = new List<TempLogSessionDetailModel>();
        foreach (var file in files)
        {
            var fileSessionDetails = await files[0].Read();
            sessionDetails.AddRange(fileSessionDetails);
        }
    }

    [Test]
    public async Task ShouldRenameFile()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        var files = await WriteLogFiles(sp);
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
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        var files = await WriteLogFiles(sp);
        files[0].Delete();
        var tempLogFolder = GetTempLogFolder(sp);
        var paths = Directory.GetFiles(tempLogFolder.Path());
        Assert.That(paths.Length, Is.EqualTo(0), "Should delete file");
    }

    [Test]
    public async Task ShouldDeserializeStartSession()
    {
        var sp = Setup("Test");
        var tempLog = sp.GetRequiredService<TempLog>();
        var files = tempLog.Files(DateTimeOffset.Now);
        foreach (var file in files)
        {
            file.Delete();
        }
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        files = await WriteLogFiles(sp);
        var session = await GetSingleSession(files);
        session.WriteToConsole();
    }

    [Test]
    public async Task ShouldDeserializeSessionDetails()
    {
        var sp = Setup();
        var files = await WriteLogFiles(sp);
        var sessionDetails = await GetSessionDetails(files);
        sessionDetails.WriteToConsole();
    }

    [Test]
    public async Task ShouldProcessFiles()
    {
        var sp = Setup();
        var tempLog = sp.GetRequiredService<TempLog>();
        await ProcessFiles(tempLog);
    }

    [Test]
    public async Task RunBenchMark()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Benchmark.RunNewVersion(1000);
        stopwatch.Stop();
        Console.WriteLine($"Time Elapsed: {stopwatch.Elapsed}");
        var sp = Setup();
        var tempLog = sp.GetRequiredService<TempLog>();
        await ProcessFiles(tempLog);
    }

    private static async Task<TempLogSessionDetailModel[]> GetSessionDetails(ITempLogFile[] files)
    {
        var sessionDetails = new List<TempLogSessionDetailModel>();
        foreach (var file in files)
        {
            var fileSessionDetails = await files[0].Read();
            sessionDetails.AddRange(fileSessionDetails);
        }
        return sessionDetails.ToArray();
    }

    private static async Task<TempLogSessionModel> GetSingleSession(ITempLogFile[] files)
    {
        Assert.That(files.Length, Is.EqualTo(1), "Should be one log file");
        var sessionDetails = await files[0].Read();
        return sessionDetails.FirstOrDefault()?.Session ?? new();
    }

    private static AppDataFolder GetTempLogFolder(IServiceProvider sp)
    {
        return sp.GetRequiredService<AppDataFolder>().WithSubFolder("TempLogs");
    }

    private IServiceProvider Setup(string envName = "Development")
    {
        var hostBuilder = new XtiHostBuilder(XtiEnvironment.Parse(envName));
        hostBuilder.Services.AddMemoryCache();
        hostBuilder.Services.AddScoped<IClock, UtcClock>();
        hostBuilder.Services.AddScoped<IAppEnvironmentContext, FakeAppEnvironmentContext>();
        hostBuilder.Services.AddSingleton<CurrentSession>();
        hostBuilder.Services.AddSingleton<XtiFolder>();
        hostBuilder.Services.AddXtiDataProtection();
        hostBuilder.Services.AddTempLogServices();
        var host = hostBuilder.Build();
        return host.Scope();
    }

    private static async Task<ITempLogFile[]> WriteLogFiles(IServiceProvider sp)
    {
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(DateTime.Now, 100);
        return logFiles;
    }

    private static async Task ProcessFiles(TempLog tempLog)
    {
        var logFiles = tempLog.Files(DateTime.Now);
        var sessionCount = 0;
        var requestCount = 0;
        foreach (var logFile in logFiles)
        {
            var sessionDetails = await logFile.Read();
            sessionDetails.WriteToConsole();
            logFile.Delete();
            sessionCount += sessionDetails.Length;
            requestCount += sessionDetails.SelectMany(s => s.RequestDetails).Count();
        }
        Console.WriteLine($"Sessions: {sessionCount}");
        Console.WriteLine($"Requests: {requestCount}");
    }

}