namespace XTI_TempLog.Abstractions;

public sealed class LogSessionDetailsRequest
{
    public LogSessionDetailsRequest()
        : this([])
    {
    }

    public LogSessionDetailsRequest(TempLogSessionDetailModel[] sessionDetails)
    {
        SessionDetails = sessionDetails;
    }

    public TempLogSessionDetailModel[] SessionDetails { get; set; }

}
