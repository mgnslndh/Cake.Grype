using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cake.Grype.Json
{
    /// <summary>
    /// Reads severities the way Grype's <c>ParseSeverity</c> does: case-insensitive names, anything else is Unknown.
    /// </summary>
    internal sealed class GrypeSeverityConverter : JsonConverter<GrypeSeverity>
    {
        public override GrypeSeverity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString()?.ToLowerInvariant() switch
                {
                    "negligible" => GrypeSeverity.Negligible,
                    "low" => GrypeSeverity.Low,
                    "medium" => GrypeSeverity.Medium,
                    "high" => GrypeSeverity.High,
                    "critical" => GrypeSeverity.Critical,
                    _ => GrypeSeverity.Unknown,
                };
            }

            reader.Skip();
            return GrypeSeverity.Unknown;
        }

        public override void Write(Utf8JsonWriter writer, GrypeSeverity value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// Reads Grype fix states (<c>fixed</c>, <c>not-fixed</c>, <c>wont-fix</c>, <c>unknown</c>); anything else is Unknown.
    /// </summary>
    internal sealed class GrypeFixStateConverter : JsonConverter<GrypeFixState>
    {
        public override GrypeFixState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString()?.ToLowerInvariant() switch
                {
                    "fixed" => GrypeFixState.Fixed,
                    "not-fixed" => GrypeFixState.NotFixed,
                    "wont-fix" => GrypeFixState.WontFix,
                    _ => GrypeFixState.Unknown,
                };
            }

            reader.Skip();
            return GrypeFixState.Unknown;
        }

        public override void Write(Utf8JsonWriter writer, GrypeFixState value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                GrypeFixState.Fixed => "fixed",
                GrypeFixState.NotFixed => "not-fixed",
                GrypeFixState.WontFix => "wont-fix",
                _ => "unknown",
            });
        }
    }
}
