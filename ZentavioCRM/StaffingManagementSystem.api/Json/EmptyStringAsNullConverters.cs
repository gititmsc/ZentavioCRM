using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZentavioCRM.Api.Json
{
    /// <summary>
    /// react-hook-form's register() binds nullable Guid/DateTime fields (AssignedToUserId,
    /// TerritoryId, LinkedCustomerId, NextFollowUpDate, ...) straight to native &lt;select&gt;
    /// and &lt;input type="date"&gt; elements. Their "nothing chosen" state is the empty string
    /// "" — not an omitted property, not JSON null — because that's what a native form control's
    /// value always is. System.Text.Json's built-in Guid/DateTime converters reject "" outright,
    /// unlike the numeric converters (see the JsonNumberHandling.AllowReadingFromString comment
    /// in Program.cs), which already tolerate "" for nullable numeric types. That mismatch is
    /// exactly what produced the generic, no-field-highlighted "One or more validation errors
    /// occurred" failure on the Lead form whenever an optional dropdown/date was left unset.
    /// These two converters close that same gap for Guid? and DateTime? globally — registered
    /// once in Program.cs — instead of patching every individual field/form. Non-nullable Guid
    /// and DateTime properties are untouched (only the Guid?/DateTime? converters below are
    /// registered), so a genuinely required Guid/DateTime field still correctly rejects "".
    /// </summary>
    public sealed class EmptyStringAsNullGuidConverter : JsonConverter<Guid?>
    {
        public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String && string.IsNullOrWhiteSpace(reader.GetString()))
            {
                return null;
            }

            return reader.GetGuid();
        }

        public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Value);
            }
        }
    }

    public sealed class EmptyStringAsNullDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String && string.IsNullOrWhiteSpace(reader.GetString()))
            {
                return null;
            }

            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Value);
            }
        }
    }
}
