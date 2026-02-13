namespace AddressFormatter.Net;

public sealed record AddressFormatterOptions
{
    public bool Abbreviate { get; init; }

    public bool AppendCountry { get; init; }

    public bool CleanupPostcode { get; init; } = true;

    public string? CountryCode { get; init; }

    public string? FallbackCountryCode { get; init; }

    // Supported values: "string" and "array".
    public string Output { get; init; } = "string";
}
