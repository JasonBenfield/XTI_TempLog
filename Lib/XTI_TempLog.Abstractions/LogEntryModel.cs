﻿namespace XTI_TempLog.Abstractions;

public sealed class LogEntryModel
{
    private string eventKey = "";
    private string caption = "";
    private string message = "";
    private string detail = "";
    private string parentEventKey = "";
    private string category = "";

    public string EventKey
    {
        get => eventKey;
        set => eventKey = value ?? "";
    }

    public int Severity { get; set; }

    public DateTimeOffset TimeOccurred { get; set; }

    public string Caption
    {
        get => caption;
        set => caption = value ?? "";
    }

    public string Message
    {
        get => message;
        set => message = value ?? "";
    }

    public string Detail
    {
        get => detail;
        set => detail = value ?? "";
    }

    public int ActualCount { get; set; }

    public string ParentEventKey
    {
        get => parentEventKey;
        set => parentEventKey = value ?? "";
    }

    public string Category
    {
        get => category;
        set => category = value ?? "";
    }
}