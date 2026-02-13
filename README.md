# AddressFormatter.Net

Address formatter library for international address rendering in .NET.

## Installation

```bash
dotnet add package AddressFormatter.Net
```

## Usage

```csharp
using AddressFormatter.Net;

var input = new Dictionary<string, object?>
{
    ["house"] = "Meat & Eat",
    ["road"] = "Vrijheidstraat",
    ["house_number"] = "63",
    ["postcode"] = "2000",
    ["city"] = "Antwerp",
    ["country_code"] = "BE"
};

var formatted = AddressFormatter.FormatString(input);
Console.WriteLine(formatted);
```

### Output lines

```csharp
var lines = AddressFormatter.FormatLines(input);
```

### Read-only input

```csharp
IReadOnlyDictionary<string, object?> input = new Dictionary<string, object?>
{
    ["road"] = "Main Street",
    ["city"] = "New York",
    ["country_code"] = "US"
};

var text = AddressFormatter.FormatString(input);
```

## Options

```csharp
var options = new AddressFormatterOptions
{
    Abbreviate = true,
    AppendCountry = true,
    CleanupPostcode = true,
    CountryCode = "US",
    FallbackCountryCode = "US",
    Output = "string" // or "array"
};

var result = AddressFormatter.Format(input, options);
```

## How to Build

```bash
dotnet restore AddressFormatter.slnx
dotnet build AddressFormatter.slnx -c Release
```
## Running tests

`ash
dotnet test AddressFormatter.slnx
```

## Notes

- `Format(...)` returns `string` by default.
- Set `Output = "array"` or use `FormatLines(...)` for `string[]` output.
- Internal behavior is covered by unit tests via `InternalsVisibleTo` for the test project.

## Benchmarks

Run benchmarks in Release mode:

```bash
dotnet run -c Release --project benchmarks/AddressFormatter.Net.Benchmarks/AddressFormatter.Net.Benchmarks.csproj
```

Quick run (short job):

```bash
dotnet run -c Release --project benchmarks/AddressFormatter.Net.Benchmarks/AddressFormatter.Net.Benchmarks.csproj -- --job short
```
