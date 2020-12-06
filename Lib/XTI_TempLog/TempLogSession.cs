using System;
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

        public TempLogSession(TempLog log, IAppEnvironmentContext appEnvironmentContext, CurrentSession currentSession, Clock clock)
        {
            this.log = log;
            this.appEnvironmentContext = appEnvironmentContext;
            this.currentSession = currentSession;
            this.clock = clock;
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

        private string requestKey;

        public async Task<StartRequestModel> StartRequest(string path)
        {
            var environment = await appEnvironmentContext.Value();
            requestKey = generateKey();
            var request = new StartRequestModel
            {
                RequestKey = requestKey,
                SessionKey = currentSession.SessionKey,
                AppType = environment.AppType,
                Path = path,
                TimeStarted = clock.Now()
            };
            var serialized = JsonSerializer.Serialize(request);
            await log.Write($"startRequest.{request.RequestKey}.log", serialized);
            return request;
        }

        private string generateKey() => Guid.NewGuid().ToString("N");

        public async Task<EndRequestModel> EndRequest()
        {
            var request = new EndRequestModel
            {
                RequestKey = requestKey,
                TimeEnded = clock.Now()
            };
            var serialized = JsonSerializer.Serialize(request);
            await log.Write($"endRequest.{request.RequestKey}.log", serialized);
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
                RequestKey = requestKey,
                TimeOccurred = clock.Now(),
                Severity = severity.Value,
                Caption = caption,
                Message = ex.Message,
                Detail = ex.StackTrace
            };
            var serialized = JsonSerializer.Serialize(tempEvent);
            await log.Write($"event.{tempEvent.EventKey}.log", serialized);
            return tempEvent;
        }

    }
}
