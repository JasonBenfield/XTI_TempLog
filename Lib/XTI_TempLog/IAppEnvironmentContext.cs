namespace XTI_TempLog;

public interface IAppEnvironmentContext
{
    Task<AppEnvironment> Value();
}