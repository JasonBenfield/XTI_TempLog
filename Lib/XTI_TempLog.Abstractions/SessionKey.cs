using System.Text.Json.Serialization;

namespace XTI_TempLog.Abstractions;

[JsonConverter(typeof(SessionKeyJsonConverter))]
public sealed record SessionKey(string ID, string UserName) : IEquatable<string>
{
    public static SessionKey Parse(string text)
    {
        string id;
        string userName;
        var index = text.LastIndexOf("|");
        if (index > -1)
        {
            id = text.Substring(0, index);
            userName = text.Substring(index + 1);
        }
        else
        {
            id = text;
            userName = "";
        }
        return new SessionKey(id, userName);
    }

    private string? formatted;

    public SessionKey()
        : this("", "")
    {
    }

    public bool IsEmpty() => string.IsNullOrWhiteSpace(ID);

    public bool IsUserNameBlank() => string.IsNullOrWhiteSpace(UserName);

    public bool HasUserName(string userName) => UserName.Equals(userName, StringComparison.OrdinalIgnoreCase);

    public string Format() =>
        formatted ??= string.IsNullOrWhiteSpace(UserName) ? ID : $"{ID}|{UserName}";

    public bool Equals(string? other) => Format().Equals(other, StringComparison.OrdinalIgnoreCase);
}
