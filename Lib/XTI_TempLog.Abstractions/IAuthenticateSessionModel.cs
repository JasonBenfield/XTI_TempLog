namespace XTI_TempLog.Abstractions;

public interface IAuthenticateSessionModel
{
    string SessionKey { get; set; }
    string UserName { get; set; }
}