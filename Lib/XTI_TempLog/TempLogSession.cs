using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using XTI_Core;

namespace XTI_TempLog
{
    public sealed class TempLogSession
    {
        private readonly TempLog log;
        private readonly IAppEnvironmentContext appEnvironmentContext;
        private readonly Clock clock;
        private readonly CurrentSession currentSession;
        private readonly ThrottledLogs throttledLogs;

        private StartRequestModel startRequestModel;
        private ThrottledLog throttledLog;
        private bool isRequestLogged;

        public TempLogSession
        (
            TempLog log,
            IAppEnvironmentContext appEnvironmentContext,
            CurrentSession currentSession,
            Clock clock,
            ThrottledLogs throttledLogs
        )
        {
            this.log = log;
            this.appEnvironmentContext = appEnvironmentContext;
            this.currentSession = currentSession;
            this.clock = clock;
            this.throttledLogs = throttledLogs;
        }

        public async Task<StartSessionModel> StartSession()
        {
            StartSessionModel session;
            var environment = await appEnvironmentContext.Value();
            if (string.IsNullOrWhiteSpace(currentSession.SessionKey))
            {
                currentSession.SessionKey = generateKey();
                session = new StartSessionModel
                {
                    SessionKey = currentSession.SessionKey,
                    TimeStarted = clock.Now(),
                    UserName = environment.UserName,
                    UserAgent = environment.UserAgent,
                    RemoteAddress = environment.RemoteAddress,
                    RequesterKey = environment.RequesterKey
                };
                var serialized = JsonSerializer.Serialize(session);
                await log.Write($"startSession.{session.SessionKey}.log", serialized);
            }
            else
            {
                session = new StartSessionModel { SessionKey = currentSession.SessionKey };
            }
            return session;
        }

        public async Task<AuthenticateSessionModel> AuthenticateSession(string userName)
        {
            var session = new AuthenticateSessionModel
            {
                SessionKey = currentSession.SessionKey,
                UserName = userName
            };
            var serialized = JsonSerializer.Serialize(session);
            await log.Write($"authSession.{session.SessionKey}.log", serialized);
            return session;
        }

        public async Task<StartRequestModel> StartRequest(string path)
        {
            var environment = await appEnvironmentContext.Value();
            startRequestModel = new StartRequestModel
            {
                RequestKey = generateKey(),
                SessionKey = currentSession.SessionKey,
                AppType = environment.AppType,
                Path = path,
                TimeStarted = clock.Now()
            };
            throttledLog = throttledLogs.GetThrottledLog(path);
            if (throttledLog.CanLogRequest())
            {
                await startRequest();
            }
            else
            {
                isRequestLogged = false;
            }
            return startRequestModel;
        }

        private async Task startRequest()
        {
            var serialized = JsonSerializer.Serialize(startRequestModel);
            await log.Write($"startRequest.{startRequestModel.RequestKey}.log", serialized);
            throttledLog.RequestLogged();
            isRequestLogged = true;
        }

        private string generateKey() => Guid.NewGuid().ToString("N");

        public async Task<EndRequestModel> EndRequest()
        {
            var request = new EndRequestModel
            {
                RequestKey = startRequestModel.RequestKey,
                TimeEnded = clock.Now()
            };
            if (isRequestLogged)
            {
                var serialized = JsonSerializer.Serialize(request);
                await log.Write($"endRequest.{request.RequestKey}.log", serialized);
            }
            return request;
        }

        public async Task<EndSessionModel> EndSession()
        {
            var request = new EndSessionModel
            {
                SessionKey = currentSession.SessionKey,
                TimeEnded = clock.Now()
            };
            var serialized = JsonSerializer.Serialize(request);
            await log.Write($"endSession.{request.SessionKey}.log", serialized);
            return request;
        }

        public async Task<LogEventModel> LogException(AppEventSeverity severity, Exception ex, string caption)
        {
            var tempEvent = new LogEventModel
            {
                EventKey = generateKey(),
                RequestKey = startRequestModel.RequestKey,
                TimeOccurred = clock.Now(),
                Severity = severity.Value,
                Caption = caption,
                Message = getExceptionMessage(ex),
                Detail = ex.StackTrace
            };
            if (throttledLog.CanLogException())
            {
                if (!isRequestLogged)
                {
                    await startRequest();
                }
                var serialized = JsonSerializer.Serialize(tempEvent);
                await log.Write($"event.{tempEvent.EventKey}.log", serialized);
                throttledLog.ExceptionLogged();
            }
            return tempEvent;
        }

        private string getExceptionMessage(Exception ex)
        {
            var messages = new List<string>();
            var currentEx = ex;
            while (currentEx != null)
            {
                messages.Add(currentEx.Message);
                currentEx = currentEx.InnerException;
            }
            return string.Join("\r\n", messages);
        }

    }
}
