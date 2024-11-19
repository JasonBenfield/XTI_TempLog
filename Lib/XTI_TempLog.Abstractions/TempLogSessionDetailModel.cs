namespace XTI_TempLog.Abstractions;

public sealed class TempLogSessionDetailModel
{
    public TempLogSessionDetailModel()
        : this(new(), [])
    {
    }

    public TempLogSessionDetailModel(TempLogSessionModel session, TempLogRequestDetailModel[] requestDetails)
    {
        Session = session;
        RequestDetails = requestDetails;
    }

    public TempLogSessionModel Session { get; set; }
    public TempLogRequestDetailModel[] RequestDetails { get; set; }
}
