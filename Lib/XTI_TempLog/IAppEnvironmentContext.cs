using System.Threading.Tasks;

namespace XTI_TempLog
{
    public interface IAppEnvironmentContext
    {
        Task<AppEnvironment> Value();
    }
}
