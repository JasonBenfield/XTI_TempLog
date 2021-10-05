﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XTI_Core;
using XTI_Core.Fakes;
using XTI_TempLog.Extensions;
using XTI_TempLog.Fakes;

namespace XTI_TempLog.Tests
{
    public sealed class ThrottledTempLogSessionOptionsTest
    {
        [Test]
        public async Task ShouldNotLogStartRequest_WhenMadeBeforeThrottledInterval()
        {
            var path = "group1/action1";
            var throttleInterval = TimeSpan.FromMinutes(1);
            var throttles = new[]
            {
                new TempLogThrottleOptions
                {
                    Path = path,
                    ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
                }
            };
            var services = setup(throttles);
            var tempLogSession = services.GetService<TempLogSession>();
            await tempLogSession.StartSession();
            await tempLogSession.StartRequest($"Test/Current/{path}");
            await tempLogSession.EndRequest();
            var clock = (FakeClock)services.GetService<Clock>();
            clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
            await tempLogSession.StartRequest($"Test/Current/{path}");
            var tempLog = services.GetService<TempLog>();
            var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
            Assert.That(startRequests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
        }

        [Test]
        public async Task ShouldNotLogStartRequest_WhenMadeBeforeThrottledIntervalUsingOptions()
        {
            var path = "group1/action1";
            var throttleInterval = TimeSpan.FromMinutes(1);
            var throttles = new[]
            {
                new TempLogThrottleOptions
                {
                    Path = path,
                    ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
                }
            };
            var services = setup(throttles);
            var tempLogSession = services.GetService<TempLogSession>();
            await tempLogSession.StartSession();
            await tempLogSession.StartRequest($"Test/Current/{path}");
            await tempLogSession.EndRequest();
            var clock = (FakeClock)services.GetService<Clock>();
            clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
            await tempLogSession.StartRequest($"Test/Current/{path}");
            var tempLog = services.GetService<TempLog>();
            var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
            Assert.That(startRequests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
        }

        [Test]
        [TestCase("group1/action1"), TestCase("group1/action2")]
        public async Task ShouldThrottlePathBasedOnRegularExpression(string path)
        {
            var throttleInterval = TimeSpan.FromMinutes(1);
            var throttles = new[]
            {
                new TempLogThrottleOptions
                {
                    Path = "group1/action\\d+$",
                    ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
                }
            };
            var services = setup(throttles);
            var tempLogSession = services.GetService<TempLogSession>();
            await tempLogSession.StartSession();
            await tempLogSession.StartRequest($"Test/Current/{path}");
            await tempLogSession.EndRequest();
            var clock = (FakeClock)services.GetService<Clock>();
            clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
            await tempLogSession.StartRequest($"Test/Current/{path}");
            var tempLog = services.GetService<TempLog>();
            var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
            Assert.That(startRequests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
        }

        [Test]
        public async Task ShouldNotLogEndRequest_WhenMadeBeforeThrottledInterval()
        {
            var path = "group1/action1";
            var throttleInterval = TimeSpan.FromMinutes(1);
            var throttles = new[]
            {
                new TempLogThrottleOptions
                {
                    Path = path,
                    ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
                }
            };
            var services = setup(throttles);
            var tempLogSession = services.GetService<TempLogSession>();
            await tempLogSession.StartSession();
            await tempLogSession.StartRequest(path);
            await tempLogSession.EndRequest();
            var clock = (FakeClock)services.GetService<Clock>();
            clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
            await tempLogSession.StartRequest(path);
            await tempLogSession.EndRequest();
            var tempLog = services.GetService<TempLog>();
            var endRequestFiles = tempLog.EndRequestFiles(clock.Now()).ToArray();
            Assert.That(endRequestFiles.Length, Is.EqualTo(1), "Should not log second end request within the throttle interval");
        }

        [Test]
        public async Task ShouldLogStartRequest_WhenPathIsNotThrottled()
        {
            var throttleInterval = TimeSpan.FromMinutes(1);
            var throttles = new[]
            {
                new TempLogThrottleOptions
                {
                    Path = "group1/action1",
                    ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
                }
            };
            var services = setup(throttles);
            var anotherPath = "group1/action2";
            var tempLogSession = services.GetService<TempLogSession>();
            await tempLogSession.StartSession();
            await tempLogSession.StartRequest(anotherPath);
            await tempLogSession.EndRequest();
            var clock = (FakeClock)services.GetService<Clock>();
            clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
            await tempLogSession.StartRequest(anotherPath);
            var tempLog = services.GetService<TempLog>();
            var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
            Assert.That(startRequests.Length, Is.EqualTo(2), "Should log when path is not throttled");
        }

        [Test]
        public async Task ShouldLogStartRequest_WhenMadeAfterThrottledInterval()
        {
            var path = "group1/action1";
            var throttleInterval = TimeSpan.FromMinutes(1);
            var throttles = new[]
            {
                new TempLogThrottleOptions
                {
                    Path = path,
                    ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
                }
            };
            var services = setup(throttles);
            var tempLogSession = services.GetService<TempLogSession>();
            await tempLogSession.StartSession();
            await tempLogSession.StartRequest($"Test/Current/{path}");
            await tempLogSession.EndRequest();
            var clock = (FakeClock)services.GetService<Clock>();
            clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
            await tempLogSession.StartRequest($"Test/Current/{path}");
            await tempLogSession.EndRequest();
            clock.Add(TimeSpan.FromSeconds(2));
            await tempLogSession.StartRequest($"Test/Current/{path}");
            await tempLogSession.EndRequest();
            var tempLog = services.GetService<TempLog>();
            var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
            Assert.That(startRequests.Length, Is.EqualTo(2), "Should log start request after the throttle interval");
        }

        [Test]
        public async Task ShouldNotLogException_WhenMadeBeforeThrottledInterval()
        {
            var throttleInterval = TimeSpan.FromMinutes(1);
            var path = "group1/action1";
            var throttles = new[]
            {
                new TempLogThrottleOptions
                {
                    Path = path,
                    ThrottleExceptionInterval = (int)throttleInterval.TotalMilliseconds
                }
            };
            var services = setup(throttles);
            var tempLogSession = services.GetService<TempLogSession>();
            await tempLogSession.StartSession();
            await tempLogSession.StartRequest(path);
            await logException(tempLogSession);
            await tempLogSession.EndRequest();
            var clock = (FakeClock)services.GetService<Clock>();
            clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
            await tempLogSession.StartRequest(path);
            await logException(tempLogSession);
            await tempLogSession.EndRequest();
            var tempLog = services.GetService<TempLog>();
            var logEventFiles = tempLog.LogEventFiles(clock.Now()).ToArray();
            Assert.That(logEventFiles.Length, Is.EqualTo(1), "Should not log second event when it happens before the throttle interval");
        }

        [Test]
        public async Task ShouldLogException_WhenMadeAfterThrottledInterval()
        {
            var path = "group1/action1";
            var throttleInterval = TimeSpan.FromMinutes(1);
            var throttles = new[]
            {
                new TempLogThrottleOptions
                {
                    Path = path,
                    ThrottleExceptionInterval = (int)throttleInterval.TotalMilliseconds
                }
            };
            var services = setup(throttles);
            var tempLogSession = services.GetService<TempLogSession>();
            await tempLogSession.StartSession();
            await tempLogSession.StartRequest($"Test/Current/{path}");
            await logException(tempLogSession);
            await tempLogSession.EndRequest();
            var clock = (FakeClock)services.GetService<Clock>();
            clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
            await tempLogSession.StartRequest($"Test/Current/{path}");
            await logException(tempLogSession);
            await tempLogSession.EndRequest();
            clock.Add(TimeSpan.FromSeconds(2));
            await tempLogSession.StartRequest($"Test/Current/{path}");
            await logException(tempLogSession);
            await tempLogSession.EndRequest();
            var tempLog = services.GetService<TempLog>();
            var logEventFiles = tempLog.LogEventFiles(clock.Now()).ToArray();
            Assert.That(logEventFiles.Length, Is.EqualTo(2), "Should log exception after the throttle interval");
        }

        [Test]
        public async Task ShouldLogThrottledStartRequest_WhenExceptionIsLogged()
        {
            var throttleInterval = TimeSpan.FromMinutes(1);
            var path = "group1/action1";
            var throttles = new[]
            {
                new TempLogThrottleOptions
                {
                    Path = path,
                    ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
                }
            };
            var services = setup(throttles);
            var tempLogSession = services.GetService<TempLogSession>();
            await tempLogSession.StartSession();
            await tempLogSession.StartRequest(path);
            await tempLogSession.EndRequest();
            var clock = (FakeClock)services.GetService<Clock>();
            clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
            await tempLogSession.StartRequest(path);
            await logException(tempLogSession);
            await tempLogSession.EndRequest();
            var tempLog = services.GetService<TempLog>();
            var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
            Assert.That(startRequests.Length, Is.EqualTo(2), "Should log throttled start request when logging an event");
        }

        [Test]
        public async Task ShouldLogThrottledEndRequest_WhenExceptionIsLogged()
        {
            var throttleInterval = TimeSpan.FromMinutes(1);
            var path = "group1/action1";
            var throttles = new[]
            {
                new TempLogThrottleOptions
                {
                    Path = path,
                    ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
                }
            };
            var services = setup(throttles);
            var tempLogSession = services.GetService<TempLogSession>();
            await tempLogSession.StartSession();
            await tempLogSession.StartRequest(path);
            await tempLogSession.EndRequest();
            var clock = (FakeClock)services.GetService<Clock>();
            clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
            await tempLogSession.StartRequest(path);
            await logException(tempLogSession);
            await tempLogSession.EndRequest();
            var tempLog = services.GetService<TempLog>();
            var endRequests = tempLog.EndRequestFiles(clock.Now()).ToArray();
            Assert.That(endRequests.Length, Is.EqualTo(2), "Should log throttled end request when logging an event");
        }

        private static async Task logException(TempLogSession tempLogSession)
        {
            try
            {
                throw new Exception("Test");
            }
            catch (Exception ex)
            {
                await tempLogSession.LogException
                (
                    AppEventSeverity.Values.CriticalError,
                    ex,
                    "An unexpected error occurred"
                );
            }
        }

        private IServiceProvider setup(IEnumerable<TempLogThrottleOptions> throttles)
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Test");
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices
                (
                    services =>
                    {
                        services.AddFakeTempLogServices();
                        services.AddSingleton<Clock, FakeClock>();
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
            var options = scope.ServiceProvider.GetService<IOptions<TempLogOptions>>();
            options.Value.Throttles = throttles.ToArray();
            return scope.ServiceProvider;
        }
    }
}
