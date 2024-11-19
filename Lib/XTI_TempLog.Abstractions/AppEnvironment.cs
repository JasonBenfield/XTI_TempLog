namespace XTI_TempLog.Abstractions;

public sealed record AppEnvironment
(
    string UserName, 
    string RequesterKey, 
    string RemoteAddress, 
    string UserAgent, 
    int InstallationID
);
