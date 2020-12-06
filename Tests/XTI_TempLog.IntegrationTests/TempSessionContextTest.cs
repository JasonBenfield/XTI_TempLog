using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using XTI_Core;
using XTI_TempLog.Extensions;
using XTI_Secrets.Extensions;

namespace XTI_TempLog.IntegrationTests
{
    public sealed class TempSessionContextTest
    {
        [Test]
        public async Task ShouldWriteFileToTempLog()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var files = input.TempLog.StartSessionFiles(DateTime.UtcNow.AddMinutes(1)).ToArray();
            Assert.That(files.Length, Is.EqualTo(1), "Should write file to temp log");
        }

        [Test]
        public async Task ShouldRenameFile()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var files = input.TempLog.StartSessionFiles(DateTime.Now.AddMinutes(1)).ToArray();
            const string newName = "moved.txt";
            files[0].WithNewName(newName);
            var paths = Directory.GetFiles(input.TempLogFolder.Path());
            Assert.That(paths.Length, Is.EqualTo(1));
            Assert.That(Path.GetFileName(paths[0]), Is.EqualTo(newName), "Should rename file");
        }

        [Test]
        public async Task ShouldDeleteFile()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var files = input.TempLog.StartSessionFiles(DateTime.UtcNow.AddMinutes(1)).ToArray();
            files[0].Delete();
            var paths = Directory.GetFiles(input.TempLogFolder.Path());
            Assert.That(paths.Length, Is.EqualTo(0), "Should delete file");
        }

        [Test]
        public async Task ShouldDeserializeStartSession()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var startSession = await getSingleStartSession(input);
            Assert.That(startSession.SessionKey?.Trim() ?? "", Is.Not.EqualTo(""), "Should deserialize start session");
        }

        [Test]
        public async Task ShouldEncryptTempLogFile()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var paths = Directory.GetFiles(input.TempLogFolder.Path());
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
            var input = setup();
            var eventFiles = input.TempLog.LogEventFiles(DateTime.Now.AddMinutes(1));
            foreach (var eventFile in eventFiles)
            {
                var contents = await eventFile.Read();
                Console.WriteLine(contents);
            }
            var startRequestFiles = input.TempLog.StartRequestFiles(DateTime.Now.AddMinutes(1));
            foreach (var startRequestFile in startRequestFiles)
            {
                var contents = await startRequestFile.Read();
                Console.WriteLine(contents);
            }
        }

        private static async Task<StartSessionModel> getSingleStartSession(TestInput input)
        {
            var files = input.TempLog.StartSessionFiles(DateTime.Now).ToArray();
            Assert.That(files.Length, Is.EqualTo(1), "Should be one start session file");
            var serializedStartSession = await files[0].Read();
            return JsonSerializer.Deserialize<StartSessionModel>(serializedStartSession);
        }

        private TestInput setup()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices
                (
                    services =>
                    {
                        services.AddScoped<Clock, UtcClock>();
                        services.AddScoped<IAppEnvironmentContext, TestAppEnvironmentContext>();
                        services.AddSingleton<CurrentSession>();
                        services.AddSingleton
                        (
                            sp => new AppDataFolder().WithSubFolder("Test").WithSubFolder("TestTempLog")
                        );
                        services.AddXtiDataProtection();
                        services.AddTempLogServices();
                    }
                )
                .Build();
            var scope = host.Services.CreateScope();
            deleteTempLogFolder(scope.ServiceProvider.GetService<AppDataFolder>());
            return new TestInput(scope.ServiceProvider);
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

        private sealed class TestInput
        {
            public TestInput(IServiceProvider sp)
            {
                TempSessionContext = sp.GetService<TempLogSession>();
                TempLog = sp.GetService<TempLog>();
                Clock = sp.GetService<Clock>();
                AppEnvironmentContext = sp.GetService<IAppEnvironmentContext>();
                TempLogFolder = sp.GetService<AppDataFolder>().WithSubFolder("TempLogs");
            }

            public TempLogSession TempSessionContext { get; }
            public TempLog TempLog { get; }
            public Clock Clock { get; }
            public IAppEnvironmentContext AppEnvironmentContext { get; }
            public AppDataFolder TempLogFolder { get; }
        }
    }
}