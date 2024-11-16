namespace XTI_TempLog.Abstractions;

public sealed class FakeAppEnvironmentContext : IAppEnvironmentContext
{
    public FakeAppEnvironmentContext()
    {
        Environment = new AppEnvironment
        (
            "test.user",
            "AppMiddleware",
            "my-computer",
            "Windows 10",
            123
        );
    }

    public AppEnvironment Environment { get; set; }

    public Task<AppEnvironment> Value() => Task.FromResult(Environment);
}