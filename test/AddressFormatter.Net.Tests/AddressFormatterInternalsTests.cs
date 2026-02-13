using AddressFormatter.Net;

namespace AddressFormatter.Net.Tests;

public class AddressFormatterInternalsTests
{
    [Fact]
    public void DetermineCountryCode_ShouldUppercase()
    {
        var converted = AddressFormatterInternals.DetermineCountryCode(new Dictionary<string, object?>
        {
            ["country_code"] = "cz"
        });

        Assert.Equal("CZ", converted["country_code"]);
    }

    [Fact]
    public void DetermineCountryCode_ShouldConvertUkToGb()
    {
        var converted = AddressFormatterInternals.DetermineCountryCode(new Dictionary<string, object?>
        {
            ["country_code"] = "UK"
        });

        Assert.Equal("GB", converted["country_code"]);
    }

    [Fact]
    public void DetermineCountryCode_ShouldApplyUseCountry()
    {
        var converted = AddressFormatterInternals.DetermineCountryCode(new Dictionary<string, object?>
        {
            ["country_code"] = "LI"
        });

        Assert.Equal("CH", converted["country_code"]);
    }

    [Fact]
    public void NormalizeComponentKeys_ShouldNormalizeCamelCase()
    {
        var converted = AddressFormatterInternals.NormalizeComponentKeys(new Dictionary<string, object?>
        {
            ["houseNumber"] = "string"
        });

        Assert.True(converted.ContainsKey("house_number"));
        Assert.False(converted.ContainsKey("houseNumber"));
    }

    [Fact]
    public void ApplyAliases_ShouldApplyStreetNumberAlias()
    {
        var converted = AddressFormatterInternals.ApplyAliases(new Dictionary<string, object?>
        {
            ["street_number"] = 123
        });

        Assert.Equal(123, converted["house_number"]);
    }

    [Fact]
    public void GetStateCode_ShouldReturnStateCode()
    {
        Assert.Equal("AL", AddressFormatterInternals.GetStateCode("Alabama", "US"));
    }

    [Fact]
    public void GetCountyCode_ShouldReturnCountyCode()
    {
        Assert.Equal("AL", AddressFormatterInternals.GetCountyCode("Alessandria", "IT"));
    }

    [Fact]
    public void CleanupInput_ShouldUseStateAsCountryWhenCountryIsNumeric()
    {
        var converted = AddressFormatterInternals.CleanupInput(new Dictionary<string, object?>
        {
            ["country"] = 123,
            ["state"] = "Slovakia"
        });

        Assert.Equal("Slovakia", converted["country"]);
    }

    [Fact]
    public void CleanupInput_ShouldDropLongPostcode()
    {
        var converted = AddressFormatterInternals.CleanupInput(new Dictionary<string, object?>
        {
            ["postcode"] = "abcdefghijklmnopqrstuvwxyz"
        });

        Assert.False(converted.ContainsKey("postcode"));
    }

    [Fact]
    public void CleanupInput_ShouldApplyReplacements()
    {
        var converted = AddressFormatterInternals.CleanupInput(
            new Dictionary<string, object?>
            {
                ["stadt"] = "Stadtteil Hamburg",
                ["city"] = "Alt-Berlin",
                ["place"] = "Alt-Berlin",
                ["platz"] = "Bonn"
            },
            new List<string[]>
            {
                new[] { "^Stadtteil ", "" },
                new[] { "city=Alt-Berlin", "Berlin" },
                new[] { "platz=Alt-Berlin", "Berlin" }
            });

        Assert.Equal("Hamburg", converted["stadt"]);
        Assert.Equal("Berlin", converted["city"]);
        Assert.Equal("Alt-Berlin", converted["place"]);
        Assert.Equal("Bonn", converted["platz"]);
    }

    [Fact]
    public void RenderTemplate_ShouldRenderRoad()
    {
        var render = AddressFormatterInternals.RenderTemplate(new System.Text.Json.Nodes.JsonObject(),
            new Dictionary<string, object?>
            {
                ["road"] = "House"
            });

        Assert.Equal("House\n", render);
    }
}
