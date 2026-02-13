using BenchmarkDotNet.Attributes;

namespace AddressFormatter.Net.Benchmarks;

[MemoryDiagnoser]
public class AddressFormattingBenchmarks
{
    private readonly IReadOnlyDictionary<string, object?> _basicInput = new Dictionary<string, object?>
    {
        ["house"] = "Meat & Eat",
        ["road"] = "Vrijheidstraat",
        ["house_number"] = "63",
        ["postcode"] = "2000",
        ["city"] = "Antwerp",
        ["country_code"] = "BE"
    };

    private readonly IReadOnlyDictionary<string, object?> _richInput = new Dictionary<string, object?>
    {
        ["house"] = "Municipal Building",
        ["road"] = "Main Street",
        ["house_number"] = "100",
        ["postcode"] = "10001",
        ["city"] = "New York",
        ["state"] = "New York",
        ["country_code"] = "US",
        ["country"] = "United States"
    };

    [Benchmark]
    public string FormatString_Basic()
    {
        return AddressFormatter.FormatString(_basicInput);
    }

    [Benchmark]
    public string[] FormatLines_Basic()
    {
        return AddressFormatter.FormatLines(_basicInput);
    }

    [Benchmark]
    public string FormatString_WithOptions()
    {
        return AddressFormatter.FormatString(_richInput, new AddressFormatterOptions
        {
            Abbreviate = true,
            AppendCountry = true,
            CleanupPostcode = true
        });
    }
}
