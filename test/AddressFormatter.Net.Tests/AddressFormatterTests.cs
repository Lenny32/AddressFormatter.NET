using AddressFormatter.Net;

namespace AddressFormatter.Net.Tests;

public class AddressFormatterTests
{
    [Fact]
    public void Format_ShouldWorkForFullExample()
    {
        var formatted = (string)AddressFormatter.Format(new Dictionary<string, object?>
        {
            ["city"] = "Antwerp",
            ["city_district"] = "Antwerpen",
            ["countryName"] = "Belgium",
            ["countryCode"] = "be",
            ["county"] = "Antwerp",
            ["houseNumber"] = 63,
            ["neighbourhood"] = "Sint-Andries",
            ["postcode"] = 2000,
            ["restaurant"] = "Meat & Eat",
            ["road"] = "Vrijheidstraat",
            ["state"] = "Flanders"
        });

        Assert.Equal("Meat & Eat\nVrijheidstraat 63\n2000 Antwerp\nBelgium\n", formatted);
    }

    [Fact]
    public void Format_ShouldWorkForMinimalExample()
    {
        var formatted = (string)AddressFormatter.Format(new Dictionary<string, object?>
        {
            ["road"] = "Vrijheidstraat"
        });

        Assert.Equal("Vrijheidstraat\n", formatted);
    }

    [Fact]
    public void Format_ShouldUseCountryCodeFromOptions()
    {
        var formatted = (string)AddressFormatter.Format(new Dictionary<string, object?>
        {
            ["city"] = "Antwerp",
            ["country"] = "Belgium",
            ["country_code"] = "be",
            ["house_number"] = 63,
            ["postcode"] = 2000,
            ["restaurant"] = "Meat & Eat",
            ["road"] = "Vrijheidstraat",
            ["state"] = "Flanders"
        }, new AddressFormatterOptions
        {
            CountryCode = "US"
        });

        Assert.Equal("Meat & Eat\n63 Vrijheidstraat\nAntwerp, Flanders 2000\nBelgium\n", formatted);
    }

    [Fact]
    public void Format_ShouldUseFallbackCountryCode()
    {
        var formatted = (string)AddressFormatter.Format(new Dictionary<string, object?>
        {
            ["city"] = "Antwerp",
            ["country"] = "Belgium",
            ["country_code"] = "yu",
            ["house_number"] = 63,
            ["postcode"] = 2000,
            ["restaurant"] = "Meat & Eat",
            ["road"] = "Vrijheidstraat",
            ["state"] = "Flanders"
        }, new AddressFormatterOptions
        {
            FallbackCountryCode = "US"
        });

        Assert.Equal("Meat & Eat\n63 Vrijheidstraat\nAntwerp, Flanders 2000\nBelgium\n", formatted);
    }

    [Fact]
    public void Format_ShouldReturnArrayWhenRequested()
    {
        var formatted = (string[])AddressFormatter.Format(new Dictionary<string, object?>
        {
            ["city"] = "Antwerp",
            ["country"] = "Belgium",
            ["country_code"] = "be",
            ["house_number"] = 63,
            ["postcode"] = 2000,
            ["restaurant"] = "Meat & Eat",
            ["road"] = "Vrijheidstraat",
            ["state"] = "Flanders"
        }, new AddressFormatterOptions
        {
            CountryCode = "US",
            Output = "array"
        });

        Assert.Equal(4, formatted.Length);
        Assert.Equal("Meat & Eat", formatted[0]);
        Assert.Equal("63 Vrijheidstraat", formatted[1]);
        Assert.Equal("Antwerp, Flanders 2000", formatted[2]);
        Assert.Equal("Belgium", formatted[3]);
    }

    [Fact]
    public void Format_ShouldAppendCountryWhenRequested()
    {
        var formatted = (string[])AddressFormatter.Format(new Dictionary<string, object?>
        {
            ["city"] = "Antwerp",
            ["countryCode"] = "be",
            ["county"] = "Antwerp",
            ["house_number"] = 63,
            ["neighbourhood"] = "Sint-Andries",
            ["postcode"] = 2000,
            ["restaurant"] = "Meat & Eat",
            ["road"] = "Vrijheidstraat",
            ["state"] = "Flanders"
        }, new AddressFormatterOptions
        {
            AppendCountry = true,
            Output = "array"
        });

        Assert.Equal("Belgium", formatted[3]);
    }

    [Fact]
    public void Format_ShouldNotCleanupPostcodeWhenDisabled()
    {
        var formatted = (string[])AddressFormatter.Format(new Dictionary<string, object?>
        {
            ["city"] = "Berlin",
            ["countryCode"] = "de",
            ["country"] = "Germany",
            ["postcode"] = "10999,10999",
            ["road"] = "Glogauer Straße"
        }, new AddressFormatterOptions
        {
            Output = "array",
            CleanupPostcode = false
        });

        Assert.Equal("10999,10999 Berlin", formatted[1]);
    }

    [Fact]
    public void Format_ShouldAbbreviateUsAvenue()
    {
        var formatted = (string)AddressFormatter.Format(new Dictionary<string, object?>
        {
            ["country_code"] = "US",
            ["house_number"] = "301",
            ["road"] = "Hamilton Avenue",
            ["city"] = "Palo Alto",
            ["postcode"] = "94303",
            ["state"] = "California",
            ["country"] = "United States"
        }, new AddressFormatterOptions
        {
            Abbreviate = true
        });

        Assert.Equal("301 Hamilton Ave\nPalo Alto, CA 94303\nUnited States of America\n", formatted);
    }
}
