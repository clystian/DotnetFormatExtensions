# Formatextension.Analyzers

A Roslyn analyzer that enforces multi-line formatting for C# initializers. Integrates seamlessly with `dotnet format analyzers` to automatically reformat single-line initializers into a clean, vertical layout.

## Rule

| ID | Severity | Description |
|---|---|---|
| `FMT0001` | `warning` | Initializer with multiple elements should have each element on its own line |

## Installation

```bash
dotnet add package Formatextension.Analyzers
```

Or add a `PackageReference` to your `.csproj`:

```xml
<PackageReference Include="Formatextension.Analyzers" Version="1.0.0" PrivateAssets="all" />
```

The package adds zero runtime dependencies — it only runs at build time and during `dotnet format`.

## Configuration

Enable the rule in your `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.FMT0001.severity = warning
```

Supported severity levels: `silent`, `suggestion`, `warning`, `error`.

## Usage

### Build-time diagnostics

The analyzer runs automatically during `dotnet build`. Violations appear as warnings:

```
warning FMT0001: Initializer with multiple elements should have each element on its own line
```

### Auto-fix with dotnet format

Apply the code fix across your project:

```bash
# Preview changes without modifying files
dotnet format analyzers --diagnostics FMT0001 --verify-no-changes

# Apply fixes
dotnet format analyzers --diagnostics FMT0001
```

## Examples

### Object initializer

**Before:**
```csharp
var config = new AppConfig { Host = "localhost", Port = 8080, Timeout = 30 };
```

**After:**
```csharp
var config = new AppConfig
{
    Host = "localhost",
    Port = 8080,
    Timeout = 30
};
```

### Array initializer

**Before:**
```csharp
var numbers = new int[] { 1, 2, 3, 4, 5 };
```

**After:**
```csharp
var numbers = new int[]
{
    1,
    2,
    3,
    4,
    5
};
```

### Collection initializer

**Before:**
```csharp
var tags = new List<string> { "csharp", "roslyn", "analyzer" };
```

**After:**
```csharp
var tags = new List<string>
{
    "csharp",
    "roslyn",
    "analyzer"
};
```

### Nested initializers

**Before:**
```csharp
var graph = new Dictionary<string, Node>
{
    ["n1"] = new Node { Id = 101, Channels = new List<string> { "A", "B" } }
};
```

**After:**
```csharp
var graph = new Dictionary<string, Node>
{
    ["n1"] = new Node
    {
        Id = 101,
        Channels = new List<string>
        {
            "A",
            "B"
        }
    }
};
```

## Edge Cases

### Multidimensional arrays — preserved

Inner initializers of multidimensional arrays are **not** flagged to prevent extreme vertical bloating of matrix data:

```csharp
var matrix = new double[2, 3]
{
    { 1.1, 1.2, 1.3 },
    { 2.1, 2.2, 2.3 }
};
```

Cell-level `{ 1.1, 1.2, 1.3 }` stays compact. The row-level initializer is still validated.

### Jagged arrays — expanded

Jagged array elements are independent allocations and **are** expanded:

```csharp
// Before
var jagged = new int[][] { new int[] { 1, 2, 3 } };

// After
var jagged = new int[][]
{
    new int[]
    {
        1,
        2,
        3
    }
};
```

### Single-element initializers — ignored

Initializers with only one element are always compliant and never flagged:

```csharp
var dict = new Dictionary<string, int> { ["only"] = 0 }; // OK
var arr = new int[] { 42 };                              // OK
```

## Compatibility

| Component | Version |
|---|---|
| Target framework | `netstandard2.0` |
| Roslyn | `4.12.0` |
| .NET SDK | `8.0+` |
| Visual Studio | `2022 17.8+` |
| Rider | `2024.1+` |

## Development

```bash
dotnet restore
dotnet build
dotnet test
```

### Release

Push a version tag to trigger the CI/CD pipeline:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The workflow builds, tests, packs, and publishes to NuGet.org automatically.

## License

MIT
