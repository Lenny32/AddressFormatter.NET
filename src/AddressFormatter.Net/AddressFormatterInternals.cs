using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AddressFormatter.Net;

internal static partial class AddressFormatterInternals
{
    private static readonly HashSet<string> SmallDistrictCountries = new(StringComparer.Ordinal)
    {
        "BR", "CR", "ES", "NI", "PY", "RO", "TG", "TM", "XK"
    };

    private static readonly HashSet<string> ValidReplacementComponents = new(StringComparer.Ordinal)
    {
        "state"
    };

    private static readonly (string Alias, string Name)[] Aliases = TemplateData.Aliases
        .Select(a => (Alias: a?["alias"]?.GetValue<string>(), Name: a?["name"]?.GetValue<string>()))
        .Where(a => !string.IsNullOrEmpty(a.Alias) && !string.IsNullOrEmpty(a.Name))
        .Select(a => (a.Alias!, a.Name!))
        .ToArray();

    private static readonly (string Alias, string Name)[] AliasesWithoutDistrict = Aliases
        .Where(a => a.Alias != "district")
        .ToArray();

    private static readonly (string Alias, string Name)[] AliasesWithDistrictAsStateDistrict =
    [.. AliasesWithoutDistrict, ("district", "state_district")];

    private static readonly HashSet<string> KnownComponents = Aliases
        .Select(a => a.Alias)
        .ToHashSet(StringComparer.Ordinal);

    private static readonly (Regex Regex, string Dest)[] CleanupRenderReplacements =
    [
        (CleanupTrailingPunctuationWhitespaceRegex(), string.Empty),
        (CleanupLeadingPunctuationWhitespaceRegex(), string.Empty),
        (CleanupDashPrefixRegex(), string.Empty),
        (CleanupDoubleCommaWithWhitespaceRegex(), ", "),
        (CleanupWhitespaceAroundCommaRegex(), ", "),
        (CleanupRepeatedSpacesRegex(), " "),
        (CleanupSpaceBeforeNewlineRegex(), "\n"),
        (CleanupLeadingCommaAfterNewlineRegex(), "\n"),
        (CleanupRepeatedCommasRegex(), ","),
        (CleanupCommaBeforeNewlineRegex(), "\n"),
        (CleanupIndentAfterNewlineRegex(), "\n"),
        (CleanupRepeatedNewlinesRegex(), "\n")
    ];

    internal static Dictionary<string, object?> DetermineCountryCode(
        Dictionary<string, object?> input,
        string? fallbackCountryCode = null)
    {
        var countryCode = GetString(input, "country_code")?.ToUpperInvariant();

        if (countryCode is not null && !TemplateData.Templates.ContainsKey(countryCode) && !string.IsNullOrWhiteSpace(fallbackCountryCode))
        {
            countryCode = fallbackCountryCode.ToUpperInvariant();
        }

        if (string.IsNullOrEmpty(countryCode) || countryCode.Length != 2)
        {
            return input;
        }

        if (countryCode == "UK")
        {
            countryCode = "GB";
        }

        if (TemplateData.Templates[countryCode] is JsonObject template && template["use_country"] is not null)
        {
            countryCode = template["use_country"]!.GetValue<string>().ToUpperInvariant();

            if (template["change_country"] is not null)
            {
                var newCountry = template["change_country"]!.GetValue<string>();
                var componentMatch = Regex.Match(newCountry, @"\$(\w*)");
                if (componentMatch.Success)
                {
                    var component = componentMatch.Groups[1].Value;
                    var replacement = GetString(input, component) ?? string.Empty;
                    newCountry = new Regex($@"\${component}").Replace(newCountry, replacement, 1);
                }

                input["country"] = newCountry;
            }

            if (template["add_component"] is not null)
            {
                var addComponent = template["add_component"]!.GetValue<string>();
                if (addComponent.Contains('='))
                {
                    var split = addComponent.Split('=', 2);
                    if (ValidReplacementComponents.Contains(split[0]))
                    {
                        input[split[0]] = split[1];
                    }
                }
            }
        }

        if (countryCode == "NL" && GetString(input, "state") is { } state)
        {
            if (state == "Curaçao")
            {
                countryCode = "CW";
                input["country"] = "Curaçao";
            }
            else if (Regex.IsMatch(state, "sint maarten", RegexOptions.IgnoreCase))
            {
                countryCode = "SX";
                input["country"] = "Sint Maarten";
            }
            else if (Regex.IsMatch(state, "aruba", RegexOptions.IgnoreCase))
            {
                countryCode = "AW";
                input["country"] = "Aruba";
            }
        }

        input["country_code"] = countryCode;
        return input;
    }

    internal static Dictionary<string, object?> NormalizeComponentKeys(Dictionary<string, object?> input)
    {
        var inputKeys = input.Keys.ToList();
        foreach (var key in inputKeys)
        {
            var snaked = ToSnakeCase(key);
            if (KnownComponents.Contains(snaked) && !IsTruthy(GetValue(input, snaked)))
            {
                if (IsTruthy(GetValue(input, key)))
                {
                    input[snaked] = input[key];
                }

                input.Remove(key);
            }
        }

        return input;
    }

    internal static Dictionary<string, object?> ApplyAliases(Dictionary<string, object?> input)
    {
        var inputKeys = input.Keys.ToList();
        var countryCode = GetString(input, "country_code");
        var tailoredAliases = !string.IsNullOrEmpty(countryCode) && SmallDistrictCountries.Contains(countryCode)
            ? Aliases
            : AliasesWithDistrictAsStateDistrict;

        foreach (var key in inputKeys)
        {
            foreach (var alias in tailoredAliases)
            {
                if (alias.Alias != key)
                {
                    continue;
                }

                if (!IsTruthy(GetValue(input, alias.Name)))
                {
                    input[alias.Name] = input[alias.Alias];
                }

                break;
            }
        }

        return input;
    }

    internal static string? GetStateCode(string state, string countryCode)
    {
        if (TemplateData.StateCodes[countryCode] is not JsonArray entries)
        {
            return null;
        }

        foreach (var node in entries)
        {
            if (node is not JsonObject entry)
            {
                continue;
            }

            var name = entry["name"];
            if (name is JsonValue)
            {
                var stateName = name!.GetValue<string>();
                if (stateName.Equals(state, StringComparison.OrdinalIgnoreCase))
                {
                    return entry["key"]?.GetValue<string>();
                }

                continue;
            }

            if (name is JsonObject variants)
            {
                foreach (var variant in variants)
                {
                    if (string.Equals(variant.Value?.GetValue<string>(), state, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry["key"]?.GetValue<string>();
                    }
                }
            }
        }

        return null;
    }

    internal static string? GetCountyCode(string county, string countryCode)
    {
        if (TemplateData.CountyCodes[countryCode] is not JsonArray entries)
        {
            return null;
        }

        foreach (var node in entries)
        {
            if (node is not JsonObject entry)
            {
                continue;
            }

            var name = entry["name"];
            if (name is JsonValue)
            {
                var countyName = name!.GetValue<string>();
                if (countyName.Equals(county, StringComparison.OrdinalIgnoreCase))
                {
                    return entry["key"]?.GetValue<string>();
                }

                continue;
            }

            if (name is JsonObject variants)
            {
                foreach (var variant in variants)
                {
                    var value = variant.Value?.GetValue<string>();
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    if (Regex.IsMatch(county, value, RegexOptions.IgnoreCase))
                    {
                        return entry["key"]?.GetValue<string>();
                    }
                }
            }
        }

        return null;
    }

    internal static Dictionary<string, object?> CleanupInput(
        Dictionary<string, object?> input,
        List<string[]>? replacements = null,
        AddressFormatterOptions? options = null)
    {
        options ??= new AddressFormatterOptions();
        var inputKeys = input.Keys.ToList();

        if (GetValue(input, "country") is sbyte or byte or short or ushort or int or uint or long or ulong && IsTruthy(GetValue(input, "state")))
        {
            input["country"] = input["state"];
            input.Remove("state");
        }

        if (replacements is { Count: > 0 })
        {
            foreach (var key in inputKeys)
            {
                foreach (var replacement in replacements)
                {
                    var componentRegex = new Regex($"^{Regex.Escape(key)}=");
                    var src = replacement[0];
                    var dest = replacement[1];

                    if (componentRegex.IsMatch(src))
                    {
                        var valueToMatch = componentRegex.Replace(src, string.Empty);
                        var valueRegex = new Regex(valueToMatch);
                        var current = GetString(input, key) ?? string.Empty;
                        if (valueRegex.IsMatch(current))
                        {
                            input[key] = valueRegex.Replace(current, dest, 1);
                        }
                    }
                    else
                    {
                        var current = Convert.ToString(GetValue(input, key), CultureInfo.InvariantCulture) ?? string.Empty;
                        input[key] = new Regex(src).Replace(current, dest, 1);
                    }
                }
            }
        }

        if (!IsTruthy(GetValue(input, "state_code")) && GetString(input, "state") is { } state)
        {
            var code = GetStateCode(state, GetString(input, "country_code") ?? string.Empty);
            if (code is not null)
            {
                input["state_code"] = code;
            }

            if (Regex.IsMatch(state, "^washington,? d\\.?c\\.?", RegexOptions.IgnoreCase))
            {
                input["state_code"] = "DC";
                input["state"] = "District of Columbia";
                input["city"] = "Washington";
            }
        }

        if (!IsTruthy(GetValue(input, "county_code")) && GetString(input, "county") is { } county)
        {
            var countyCode = GetCountyCode(county, GetString(input, "country_code") ?? string.Empty);
            if (countyCode is not null)
            {
                input["county_code"] = countyCode;
            }
        }

        var unknownComponents = inputKeys.Where(k => !KnownComponents.Contains(k)).ToList();
        if (unknownComponents.Count > 0)
        {
            input["attention"] = string.Join(", ",
                unknownComponents.Select(k => Convert.ToString(GetValue(input, k), CultureInfo.InvariantCulture) ?? string.Empty));
        }

        if (IsTruthy(GetValue(input, "postcode")) && options.CleanupPostcode)
        {
            var postcode = Convert.ToString(GetValue(input, "postcode"), CultureInfo.InvariantCulture) ?? string.Empty;
            input["postcode"] = postcode;

            var multiCodeMatch = Regex.Match(postcode, "^(\\d{5}),\\d{5}");
            if (postcode.Length > 20)
            {
                input.Remove("postcode");
            }
            else if (Regex.IsMatch(postcode, "\\d+;\\d+"))
            {
                input.Remove("postcode");
            }
            else if (multiCodeMatch.Success)
            {
                input["postcode"] = multiCodeMatch.Groups[1].Value;
            }
        }

        var inputCountryCode = GetString(input, "country_code");
        if (options.Abbreviate && !string.IsNullOrEmpty(inputCountryCode) && TemplateData.CountryToLang[inputCountryCode] is JsonArray languages)
        {
            foreach (var languageNode in languages)
            {
                var lang = languageNode?.GetValue<string>();
                if (string.IsNullOrEmpty(lang) || TemplateData.Abbreviations[lang] is not JsonArray languageAbbreviations)
                {
                    continue;
                }

                foreach (var abbreviationNode in languageAbbreviations)
                {
                    if (abbreviationNode is not JsonObject abbreviation)
                    {
                        continue;
                    }

                    var component = abbreviation["component"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(component) || !IsTruthy(GetValue(input, component)))
                    {
                        continue;
                    }

                    var componentValue = Convert.ToString(GetValue(input, component), CultureInfo.InvariantCulture) ?? string.Empty;
                    if (abbreviation["replacements"] is not JsonArray replacementNodes)
                    {
                        continue;
                    }

                    foreach (var replacementNode in replacementNodes)
                    {
                        if (replacementNode is not JsonObject replacement)
                        {
                            continue;
                        }

                        var src = replacement["src"]?.GetValue<string>();
                        var dest = replacement["dest"]?.GetValue<string>() ?? string.Empty;
                        if (string.IsNullOrEmpty(src))
                        {
                            continue;
                        }

                        componentValue = new Regex($@"(^|\s){src}\b").Replace(componentValue, $"$1{dest}", 1);
                    }

                    input[component] = componentValue;
                }
            }
        }

        foreach (var key in input.Keys.ToList())
        {
            var value = Convert.ToString(GetValue(input, key), CultureInfo.InvariantCulture) ?? string.Empty;
            if (Regex.IsMatch(value, "^https?://", RegexOptions.IgnoreCase))
            {
                input.Remove(key);
            }
        }

        return input;
    }

    internal static JsonObject FindTemplate(Dictionary<string, object?> input)
    {
        var countryCode = GetString(input, "country_code");
        if (!string.IsNullOrEmpty(countryCode) && TemplateData.Templates[countryCode] is JsonObject countryTemplate)
        {
            return countryTemplate;
        }

        return TemplateData.Templates["default"]!.AsObject();
    }

    internal static string ChooseTemplateText(JsonObject template, Dictionary<string, object?> input)
    {
        var selected = template["address_template"]?.GetValue<string>()
                       ?? TemplateData.Templates["default"]!["address_template"]!.GetValue<string>();

        const int threshold = 2;
        var required = new[] { "road", "postcode" };
        var missingCount = required.Count(key => !IsTruthy(GetValue(input, key)));

        if (missingCount == threshold)
        {
            selected = template["fallback_template"]?.GetValue<string>()
                       ?? TemplateData.Templates["default"]!["fallback_template"]!.GetValue<string>();
        }

        return selected;
    }

    internal static string CleanupRender(string text)
    {
        static string Dedupe(IEnumerable<string> inputChunks, string glue, Func<string, string>? modifier = null)
        {
            modifier ??= s => s;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<string>();

            foreach (var rawChunk in inputChunks)
            {
                var chunk = rawChunk.Trim();
                if (chunk.Equals("new york", StringComparison.OrdinalIgnoreCase))
                {
                    seen.Add(chunk);
                    result.Add(chunk);
                    continue;
                }

                if (!seen.Contains(chunk))
                {
                    seen.Add(chunk);
                    result.Add(modifier(chunk));
                }
            }

            return string.Join(glue, result);
        }

        foreach (var replacement in CleanupRenderReplacements)
        {
            text = replacement.Regex.Replace(text, replacement.Dest, 1);
            text = Dedupe(text.Split('\n'), "\n", s => Dedupe(s.Split(", "), ", "));
        }

        return text.Trim();
    }

    internal static string RenderTemplate(JsonObject template, Dictionary<string, object?> input)
    {
        var templateText = ChooseTemplateText(template, input);

        var render = CleanupRender(RenderMustache(templateText, input));

        if (template["postformat_replace"] is JsonArray postformatReplacements)
        {
            foreach (var replacementNode in postformatReplacements)
            {
                if (replacementNode is not JsonArray replacement || replacement.Count < 2)
                {
                    continue;
                }

                var src = replacement[0]?.GetValue<string>() ?? string.Empty;
                var dest = replacement[1]?.GetValue<string>() ?? string.Empty;
                render = new Regex(src).Replace(render, dest, 1);
            }
        }

        render = CleanupRender(render);

        if (!render.Trim().Any())
        {
            render = CleanupRender(string.Join(", ", input.Values
                .Where(IsTruthy)
                .Select(v => Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty)));
        }

        return render + "\n";
    }

    private static string RenderMustache(string template, Dictionary<string, object?> input)
    {
        var withFirst = Regex.Replace(
            template,
            "\\{\\{#first\\}\\}(.*?)\\{\\{/first\\}\\}",
            match =>
            {
                var rendered = RenderVariables(match.Groups[1].Value, input);
                var possibilities = Regex.Split(rendered, "\\s*\\|\\|\\s*")
                    .Where(p => p.Length > 0)
                    .ToArray();
                return possibilities.Length > 0 ? possibilities[0] : string.Empty;
            },
            RegexOptions.Singleline);

        return RenderVariables(withFirst, input);
    }

    private static string RenderVariables(string template, Dictionary<string, object?> input)
    {
        return Regex.Replace(
            template,
            "\\{\\{\\{\\s*([a-zA-Z0-9_]+)\\s*\\}\\}\\}",
            m => Convert.ToString(GetValue(input, m.Groups[1].Value), CultureInfo.InvariantCulture) ?? string.Empty);
    }

    [GeneratedRegex("[\\},\\s]+$", RegexOptions.None, 1000)]
    private static partial Regex CleanupTrailingPunctuationWhitespaceRegex();

    [GeneratedRegex("^[,\\s]+", RegexOptions.None, 1000)]
    private static partial Regex CleanupLeadingPunctuationWhitespaceRegex();

    [GeneratedRegex("^- ", RegexOptions.None, 1000)]
    private static partial Regex CleanupDashPrefixRegex();

    [GeneratedRegex(",\\s*,", RegexOptions.None, 1000)]
    private static partial Regex CleanupDoubleCommaWithWhitespaceRegex();

    [GeneratedRegex("[ \\t]+,[ \\t]+", RegexOptions.None, 1000)]
    private static partial Regex CleanupWhitespaceAroundCommaRegex();

    [GeneratedRegex("[ \\t][ \\t]+", RegexOptions.None, 1000)]
    private static partial Regex CleanupRepeatedSpacesRegex();

    [GeneratedRegex("[ \\t]\\n", RegexOptions.None, 1000)]
    private static partial Regex CleanupSpaceBeforeNewlineRegex();

    [GeneratedRegex("\\n,", RegexOptions.None, 1000)]
    private static partial Regex CleanupLeadingCommaAfterNewlineRegex();

    [GeneratedRegex(",,+", RegexOptions.None, 1000)]
    private static partial Regex CleanupRepeatedCommasRegex();

    [GeneratedRegex(",\\n", RegexOptions.None, 1000)]
    private static partial Regex CleanupCommaBeforeNewlineRegex();

    [GeneratedRegex("\\n[ \\t]+", RegexOptions.None, 1000)]
    private static partial Regex CleanupIndentAfterNewlineRegex();

    [GeneratedRegex("\\n\\n+", RegexOptions.None, 1000)]
    private static partial Regex CleanupRepeatedNewlinesRegex();
    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        Span<char> buffer = value.Length <= 128
            ? stackalloc char[value.Length * 2]
            : new char[value.Length * 2];

        var position = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                buffer[position++] = '_';
            }

            buffer[position++] = char.ToLowerInvariant(c);
        }

        return new string(buffer[..position]);
    }

    private static object? GetValue(Dictionary<string, object?> input, string key)
    {
        return input.TryGetValue(key, out var value) ? value : null;
    }

    private static string? GetString(Dictionary<string, object?> input, string key)
    {
        return Convert.ToString(GetValue(input, key), CultureInfo.InvariantCulture);
    }

    private static bool IsTruthy(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (value is bool b)
        {
            return b;
        }

        if (value is string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        if (value is sbyte or byte or short or ushort or int or uint or long or ulong)
        {
            return Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0;
        }

        if (value is float or double or decimal)
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture) != 0.0;
        }

        return true;
    }
}



