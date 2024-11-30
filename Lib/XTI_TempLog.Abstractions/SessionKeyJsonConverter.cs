using System.Text.Json;
using System.Text.Json.Serialization;

namespace XTI_TempLog.Abstractions;

public sealed class SessionKeyJsonConverter : JsonConverter<SessionKey>
{
    public override bool HandleNull => true;

    public override SessionKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new SessionKey();
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            return SessionKey.Parse(reader.GetString() ?? "");
        }
        if(reader.TokenType == JsonTokenType.StartObject)
        {
            var id = "";
            var userName = "";
            while(reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if(reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    if (propertyName == "ID")
                    {
                        id = reader.GetString() ?? "";
                    }
                    else if(propertyName == "UserName")
                    {
                        userName = reader.GetString() ?? "";
                    }
                }
            }
            return new SessionKey(id, userName);
        }
        throw new NotSupportedException($"Unexpected JSON token '{reader.TokenType}'");
    }

    public override void Write(Utf8JsonWriter writer, SessionKey value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("ID", value.ID);
        writer.WriteString("UserName", value.UserName);
        writer.WriteEndObject();
    }
}