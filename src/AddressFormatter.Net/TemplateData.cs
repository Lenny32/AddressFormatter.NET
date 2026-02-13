using System.Reflection;
using System.Text.Json.Nodes;

namespace AddressFormatter.Net;

internal static class TemplateData
{
    internal static readonly JsonObject Templates = LoadObject("templates.json");
    internal static readonly JsonArray Aliases = LoadArray("aliases.json");
    internal static readonly JsonObject StateCodes = LoadObject("state-codes.json");
    internal static readonly JsonObject CountyCodes = LoadObject("county-codes.json");
    internal static readonly JsonObject CountryToLang = LoadObject("country-to-lang.json");
    internal static readonly JsonObject Abbreviations = LoadObject("abbreviations.json");
    internal static readonly JsonObject CountryNames = LoadObject("country-names.json");

    private static JsonObject LoadObject(string fileName)
    {
        return JsonNode.Parse(ReadEmbedded(fileName))?.AsObject()
               ?? throw new InvalidOperationException($"Template file '{fileName}' is empty.");
    }

    private static JsonArray LoadArray(string fileName)
    {
        return JsonNode.Parse(ReadEmbedded(fileName))?.AsArray()
               ?? throw new InvalidOperationException($"Template file '{fileName}' is empty.");
    }

    private static string ReadEmbedded(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"AddressFormatter.Net.Templates.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

