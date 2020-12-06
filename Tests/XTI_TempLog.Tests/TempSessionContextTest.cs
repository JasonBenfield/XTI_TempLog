using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using XTI_Core;
using XTI_Core.Fakes;
using XTI_TempLog.Fakes;

namespace XTI_TempLog.Tests
{
    public sealed class TempSessionContextTest
    {
        [Test]
        public async Task ShouldWriteSessionToLog_WhenStartingASession()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var startSession = await getSingleStartSession(input);
            Assert.That(string.IsNullOrWhiteSpace(startSession.SessionKey), Is.False, "Should create session key");
            Assert.That(startSession.TimeStarted, Is.EqualTo(input.Clock.Now()), "Should start session");
            Assert.That(startSession.UserName, Is.EqualTo(input.AppEnvironmentContext.Environment.UserName), "Should set user name from environment");
            Assert.That(startSession.RequesterKey, Is.EqualTo(input.AppEnvironmentContext.Environment.RequesterKey), "Should set requester key from environment");
            Assert.That(startSession.UserAgent, Is.EqualTo(input.AppEnvironmentContext.Environment.UserAgent), "Should set user agent from environment");
            Assert.That(startSession.RemoteAddress, Is.EqualTo(input.AppEnvironmentContext.Environment.RemoteAddress), "Should set remote address from environment");
        }

        private static async Task<StartSessionModel> getSingleStartSession(TestInput input)
        {
            var files = input.TempLog.StartSessionFiles(DateTime.Now).ToArray();
            Assert.That(files.Length, Is.EqualTo(1), "Should be one start session file");
            var serializedStartSession = await files[0].Read();
            return JsonSerializer.Deserialize<StartSessionModel>(serializedStartSession);
        }

        [Test]
        public async Task ShouldWriteRequestToLog_WhenStartingARequest()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var path = "group1/action1";
            await input.TempSessionContext.StartRequest(path);
            var startSession = await getSingleStartSession(input);
            var startRequest = await getSingleStartRequest(input);
            Assert.That(startRequest.SessionKey, Is.EqualTo(startSession.SessionKey), "Should create session key");
            Assert.That(startRequest.TimeStarted, Is.EqualTo(input.Clock.Now()), "Should start session");
            Assert.That(startRequest.Path, Is.EqualTo(path), "Should set path");
            Assert.That(string.IsNullOrWhiteSpace(startRequest.RequestKey), Is.False, "Should set request key");
            Assert.That(startRequest.AppType, Is.EqualTo("WebApp"), "Should set app key from environment");
        }

        private static async Task<StartRequestModel> getSingleStartRequest(TestInput input)
        {
            var files = input.TempLog.StartRequestFiles(DateTime.Now).ToArray();
            Assert.That(files.Length, Is.EqualTo(1), "Should be one start request file");
            var serializedStartRequest = await files[0].Read();
            return JsonSerializer.Deserialize<StartRequestModel>(serializedStartRequest);
        }

        [Test]
        public async Task ShouldWriteRequestToLog_WhenEndingARequest()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var path = "group1/action1";
            await input.TempSessionContext.StartRequest(path);
            input.Clock.Set(input.Clock.Now().AddMinutes(1));
            await input.TempSessionContext.EndRequest();
            var endRequest = await getSingleEndRequest(input);
            var startRequest = await getSingleStartRequest(input);
            Assert.That(endRequest.RequestKey, Is.EqualTo(startRequest.RequestKey), "Request key should be the same as the start request");
            Assert.That(endRequest.TimeEnded, Is.EqualTo(input.Clock.Now()), "Should set the end time");
        }

        private static async Task<EndRequestModel> getSingleEndRequest(TestInput input)
        {
            var files = input.TempLog.EndRequestFiles(input.Clock.Now()).ToArray();
            Assert.That(files.Length, Is.EqualTo(1), "Should be one end request file");
            var serializedEndRequest = await files[0].Read();
            return JsonSerializer.Deserialize<EndRequestModel>(serializedEndRequest);
        }

        [Test]
        public async Task ShouldWriteSessionToLog_WhenEndingASession()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var path = "group1/action1";
            await input.TempSessionContext.StartRequest(path);
            input.Clock.Set(input.Clock.Now().AddMinutes(1));
            await input.TempSessionContext.EndRequest();
            await input.TempSessionContext.EndSession();
            var endSession = await getSingleEndSession(input);
            var startSession = await getSingleStartSession(input);
            Assert.That(endSession.SessionKey, Is.EqualTo(startSession.SessionKey), "Should have the same session key as the start session");
            Assert.That(endSession.TimeEnded, Is.EqualTo(input.Clock.Now()), "Should set time ended");
        }

        private static async Task<EndSessionModel> getSingleEndSession(TestInput input)
        {
            var files = input.TempLog.EndSessionFiles(input.Clock.Now()).ToArray();
            Assert.That(files.Length, Is.EqualTo(1), "Should be one end session file");
            var serializedEndSession = await files[0].Read();
            return JsonSerializer.Deserialize<EndSessionModel>(serializedEndSession);
        }

        [Test]
        public async Task ShouldAuthenticateSession()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var userName = "test.user";
            await input.TempSessionContext.AuthenticateSession(userName);
            var authSession = await getSingleAuthSession(input);
            var startSession = await getSingleStartSession(input);
            Assert.That(authSession.SessionKey, Is.EqualTo(startSession.SessionKey), "Should have the same session key as the start session");
            Assert.That(authSession.UserName, Is.EqualTo(userName), "Should set user name");
        }

        private static async Task<AuthenticateSessionModel> getSingleAuthSession(TestInput input)
        {
            var files = input.TempLog.AuthSessionFiles(input.Clock.Now()).ToArray();
            Assert.That(files.Length, Is.EqualTo(1), "Should be one auth session file");
            var serializedAuthSession = await files[0].Read();
            return JsonSerializer.Deserialize<AuthenticateSessionModel>(serializedAuthSession);
        }

        [Test]
        public async Task ShouldLogError()
        {
            var input = setup();
            await input.TempSessionContext.StartSession();
            var path = "group1/action1";
            await input.TempSessionContext.StartRequest(path);
            Exception thrownException;
            try
            {
                throw new Exception("Test");
            }
            catch (Exception ex)
            {
                await input.TempSessionContext.LogException
                (
                    AppEventSeverity.Values.CriticalError,
                    ex,
                    "An unexpected error occurred"
                );
                thrownException = ex;
            }
            var requestFiles = input.TempLog.StartRequestFiles(input.Clock.Now()).ToArray();
            var request = JsonSerializer.Deserialize<StartRequestModel>(await requestFiles[0].Read());
            var logEvent = await getSingleLogEvent(input);
            Assert.That(string.IsNullOrWhiteSpace(logEvent.EventKey), Is.False, "Should create event key");
            Assert.That(logEvent.RequestKey, Is.EqualTo(request.RequestKey), "Should set request key");
            Assert.That(logEvent.Severity, Is.EqualTo(AppEventSeverity.Values.CriticalError.Value), "Should set severity");
            Assert.That(logEvent.Caption, Is.EqualTo("An unexpected error occurred"), "Should set caption");
            Assert.That(logEvent.Message, Is.EqualTo(thrownException.Message), "Should set message");
            Assert.That(logEvent.Detail, Is.EqualTo(thrownException.StackTrace), "Should set detail");
            Assert.That(logEvent.TimeOccurred, Is.EqualTo(input.Clock.Now()), "Should set time occurred to the current time");
        }

        private static async Task<LogEventModel> getSingleLogEvent(TestInput input)
        {
            var files = input.TempLog.LogEventFiles(DateTime.Now).ToArray();
            Assert.That(files.Length, Is.EqualTo(1), "Should be one log event file");
            var serializedLogEvent = await files[0].Read();
            return JsonSerializer.Deserialize<LogEventModel>(serializedLogEvent);
        }

        private TestInput setup()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices
                (
                    services =>
                    {
                        services.AddFakeTempLogServices();
                        services.AddScoped<Clock, FakeClock>();
                        services.AddScoped<IAppEnvironmentContext>(sp => new FakeAppEnvironmentContext
                        {
                            Environment = new AppEnvironment
                            (
                                "test.user", "my-computer", "10.1.0.0", "Windows 10", "WebApp"
                            )
                        });
                    }
                )
                .Build();
            var scope = host.Services.CreateScope();
            return new TestInput(scope.ServiceProvider);
        }

        private sealed class TestInput
        {
            public TestInput(IServiceProvider sp)
            {
                TempSessionContext = sp.GetService<TempLogSession>();
                TempLog = (FakeTempLog)sp.GetService<TempLog>();
                Clock = (FakeClock)sp.GetService<Clock>();
                AppEnvironmentContext = (FakeAppEnvironmentContext)sp.GetService<IAppEnvironmentContext>();
            }

            public TempLogSession TempSessionContext { get; }
            public FakeTempLog TempLog { get; }
            public FakeClock Clock { get; }
            public FakeAppEnvironmentContext AppEnvironmentContext { get; }
        }
    }
}
