namespace XTI_TempLog.Abstractions;

public interface IAppEnvironmentContext
{
    Task<AppEnvironment> Value();
}