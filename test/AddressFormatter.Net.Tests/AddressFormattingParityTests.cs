using AddressFormatter.Net;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AddressFormatter.Net.Tests;

public class AddressFormattingParityTests
{
    public static IEnumerable<object[]> AllAddressFormattingCases()
    {
        var root = FindRepositoryRoot();
        var testcasesRoot = Path.Combine(root, "address-formatter", "address-formatting", "testcases");

        if (!Directory.Exists(testcasesRoot))
        {
            throw new DirectoryNotFoundException($"Testcases folder not found: {testcasesRoot}");
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithAttemptingUnquotedStringTypeDeserialization()
            .Build();

        foreach (var testCase in LoadSuite(deserializer, testcasesRoot, "abbreviations", new AddressFormatterOptions
                 {
                     Abbreviate = true,
                     CleanupPostcode = false
                 }))
        {
            yield return new object[] { testCase.Name, testCase.Components, testCase.Options, testCase.Expected };
        }

        foreach (var testCase in LoadSuite(deserializer, testcasesRoot, "countries", new AddressFormatterOptions()))
        {
            yield return new object[] { testCase.Name, testCase.Components, testCase.Options, testCase.Expected };
        }

        foreach (var testCase in LoadSuite(deserializer, testcasesRoot, "other", new AddressFormatterOptions()))
        {
            yield return new object[] { testCase.Name, testCase.Components, testCase.Options, testCase.Expected };
        }
    }

    [Theory]
    [MemberData(nameof(AllAddressFormattingCases))]
    public void DotNetImplementation_ShouldMatch_AddressFormattingTestcase(
        string caseName,
        Dictionary<string, object?> components,
        AddressFormatterOptions options,
        string expected)
    {
        _ = caseName;
        var actual = (string)AddressFormatter.Format(components, options);
        Assert.Equal(expected, actual);
    }

    private static IEnumerable<AddressFormattingCase> LoadSuite(
        IDeserializer deserializer,
        string root,
        string suite,
        AddressFormatterOptions suiteOptions)
    {
        var suitePath = Path.Combine(root, suite);

        foreach (var file in Directory.EnumerateFiles(suitePath, "*.yaml", SearchOption.TopDirectoryOnly).OrderBy(p => p))
        {
            var yaml = File.ReadAllText(file);
            var docs = ParseYamlDocuments(deserializer, yaml);
            var relativePath = Path.GetRelativePath(root, file);

            for (var i = 0; i < docs.Count; i++)
            {
                var doc = docs[i];
                if (doc is null || !doc.TryGetValue("components", out var componentsObj) || !doc.TryGetValue("expected", out var expectedObj))
                {
                    continue;
                }

                if (componentsObj is not Dictionary<object, object?> rawComponents)
                {
                    continue;
                }

                var description = doc.TryGetValue("description", out var descriptionObj)
                    ? descriptionObj?.ToString()
                    : null;

                var caseName = string.IsNullOrWhiteSpace(description)
                    ? $"{relativePath}#{i}"
                    : $"{relativePath}#{i} - {description}";

                yield return new AddressFormattingCase(
                    caseName,
                    ConvertToInputDictionary(rawComponents),
                    suiteOptions,
                    expectedObj?.ToString() ?? string.Empty);
            }
        }
    }

    private static List<Dictionary<object, object?>?> ParseYamlDocuments(IDeserializer deserializer, string yaml)
    {
        var docs = new List<Dictionary<object, object?>?>();
        var parser = new Parser(new StringReader(yaml));

        parser.Consume<StreamStart>();
        while (!parser.Accept<StreamEnd>(out _))
        {
            docs.Add(deserializer.Deserialize<Dictionary<object, object?>>(parser));
        }

        return docs;
    }

    private static Dictionary<string, object?> ConvertToInputDictionary(Dictionary<object, object?> source)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kv in source)
        {
            var key = kv.Key.ToString() ?? string.Empty;
            result[key] = kv.Value;
        }

        return result;
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "address-formatter")) &&
                Directory.Exists(Path.Combine(dir.FullName, "AddressFormatter.NET")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test runtime path.");
    }

    private sealed record AddressFormattingCase(
        string Name,
        Dictionary<string, object?> Components,
        AddressFormatterOptions Options,
        string Expected);
}

