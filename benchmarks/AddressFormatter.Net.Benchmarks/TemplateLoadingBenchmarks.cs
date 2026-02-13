using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;

namespace AddressFormatter.Net.Benchmarks;

[MemoryDiagnoser]
public class TemplateLoadingBenchmarks
{
    private static readonly string[] ResourceFiles =
    [
        "templates.json",
        "aliases.json",
        "state-codes.json",
        "county-codes.json",
        "country-to-lang.json",
        "abbreviations.json",
        "country-names.json"
    ];

    [Benchmark]
    public int LoadAndParseTemplatesJson()
    {
        var assembly = typeof(AddressFormatter).Assembly;
        using var stream = assembly.GetManifestResourceStream("AddressFormatter.Net.Templates.templates.json")!;
    var root = JsonNode.Parse(stream)!.AsObject();
        return root.Count;
    }

    [Benchmark]
    public int LoadAndParseAllEmbeddedTemplateFiles()
    {
        var assembly = typeof(AddressFormatter).Assembly;
        var count = 0;

        foreach (var fileName in ResourceFiles)
        {
            var resourceName = $"AddressFormatter.Net.Templates.{fileName}";
            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            count += JsonNode.Parse(json) switch
            {
                JsonObject o => o.Count,
                JsonArray a => a.Count,
                _ => 0
            };
        }

        return count;
    }
}
