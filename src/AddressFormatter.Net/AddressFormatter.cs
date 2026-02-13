using System.Text.Json.Nodes;

namespace AddressFormatter.Net;

public static class AddressFormatter
{
    public static object Format(Dictionary<string, object?> input, AddressFormatterOptions? options = null)
    {
        options ??= new AddressFormatterOptions();

        var realInput = new Dictionary<string, object?>(input, StringComparer.Ordinal);
        realInput = AddressFormatterInternals.NormalizeComponentKeys(realInput);

        if (!string.IsNullOrWhiteSpace(options.CountryCode))
        {
            realInput["country_code"] = options.CountryCode;
        }

        realInput = AddressFormatterInternals.DetermineCountryCode(realInput, options.FallbackCountryCode);

        var countryCode = realInput.TryGetValue("country_code", out var codeObj) ? codeObj?.ToString() : null;
        if (options.AppendCountry
            && !string.IsNullOrEmpty(countryCode)
            && TemplateData.CountryNames[countryCode] is not null
            && !realInput.ContainsKey("country"))
        {
            realInput["country"] = TemplateData.CountryNames[countryCode]!.GetValue<string>();
        }

        realInput = AddressFormatterInternals.ApplyAliases(realInput);
        var template = AddressFormatterInternals.FindTemplate(realInput);

        var replacements = template["replace"]?.AsArray()?
            .Select(x => x?.AsArray())
            .Where(x => x is not null && x.Count >= 2)
            .Select(x => new[] { x![0]!.GetValue<string>(), x[1]!.GetValue<string>() })
            .ToList() ?? new List<string[]>();

        realInput = AddressFormatterInternals.CleanupInput(realInput, replacements, options);
        var result = AddressFormatterInternals.RenderTemplate(template, realInput);

        if (string.Equals(options.Output, "array", StringComparison.Ordinal))
        {
            return result.Split('\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        return result;
    }

    public static string FormatString(Dictionary<string, object?> input, AddressFormatterOptions? options = null)
    {
        var formatOptions = options ?? new AddressFormatterOptions();
        formatOptions = formatOptions with { Output = "string" };
        return (string)Format(input, formatOptions);
    }

    public static string[] FormatLines(Dictionary<string, object?> input, AddressFormatterOptions? options = null)
    {
        var formatOptions = options ?? new AddressFormatterOptions();
        formatOptions = formatOptions with { Output = "array" };
        return (string[])Format(input, formatOptions);
    }
}
